using Application.Abstractions;
using Application.Abstractions.Services.Fees;
using Application.Fees;
using Application.ViewModels.ClassFeeMap;
using Application.ViewModels.Invoice;
using Application.ViewModels.Payment;
using BonzerSchool.UnitTests.TestSupport;
using Domain.Entities.Fees;
using Domain.Entities.Organization;
using Domain.Entities.Students;
using Infrastructure.Persistence.Repositories.Fees;
using Infrastructure.Persistence.Repositories.Organization;
using Infrastructure.Services.Common;
using Infrastructure.Services.Fees;
using Microsoft.Extensions.Caching.Memory;

namespace BonzerSchool.UnitTests.Fees;

/// <summary>Class 5 owes Tuition 30 000 + Transport 10 000 in 2026-27 (and had other prices in 2025-26).</summary>
public class InvoiceServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private readonly InvoiceService _invoices;
    private readonly PaymentService _payments;
    private readonly ClassFeeMapService _feeMaps;
    private readonly Student _student;
    private readonly AcademicYear _year;
    private readonly FeeHead _tuition;
    private readonly Domain.Entities.Academics.Class _class;

    public InvoiceServiceTests()
    {
        _class = _env.AddClass(_env.AddBranch().Id, "Class 5");
        var lastYear = new AcademicYear { Id = Guid.NewGuid(), YearName = "2025-26", StartDate = new DateOnly(2025, 6, 1), EndDate = new DateOnly(2026, 4, 30) };
        _year = new AcademicYear { Id = Guid.NewGuid(), YearName = "2026-27", StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2027, 4, 30) };
        _tuition = new FeeHead { Id = Guid.NewGuid(), Name = "Tuition" };
        var transport = new FeeHead { Id = Guid.NewGuid(), Name = "Transport" };
        _student = new Student { Id = Guid.NewGuid(), FirstName = "Riya", BranchId = _class.BranchId, ClassId = _class.Id, AcademicYearId = _year.Id };
        _env.Db.AddRange(lastYear, _year, _tuition, transport, _student,
            new ClassFeeMap { Id = Guid.NewGuid(), ClassId = _class.Id, AcademicYearId = lastYear.Id, FeeHeadId = _tuition.Id, Amount = 99999 },
            new ClassFeeMap { Id = Guid.NewGuid(), ClassId = _class.Id, AcademicYearId = _year.Id, FeeHeadId = _tuition.Id, Amount = 30000 },
            new ClassFeeMap { Id = Guid.NewGuid(), ClassId = _class.Id, AcademicYearId = _year.Id, FeeHeadId = transport.Id, Amount = 10000 });
        _env.Db.SaveChanges();

        var cache = new CacheService(new MemoryCache(new MemoryCacheOptions()), _env.TenantContext);
        _invoices = new InvoiceService(TestEnvironment.Logger<InvoiceService>(), new InvoiceRepository(_env.Db, TestEnvironment.Logger<InvoiceRepository>()),
            Substitute.For<IUnitOfWork>(), _env.Mapper, cache, _env.Db, Substitute.For<IFeeStudentService>());
        _payments = new PaymentService(TestEnvironment.Logger<PaymentService>(), new PaymentRepository(_env.Db, TestEnvironment.Logger<PaymentRepository>()),
            Substitute.For<IUnitOfWork>(), _env.Mapper, _env.Db, new FixedClock(new DateTimeOffset(2026, 11, 3, 9, 0, 0, TimeSpan.Zero)));
        _feeMaps = new ClassFeeMapService(TestEnvironment.Logger<ClassFeeMapService>(), new ClassFeeMapRepository(_env.Db, TestEnvironment.Logger<ClassFeeMapRepository>()),
            _env.Mapper, _env.Cache, _env.AcademicStructure(), new AcademicYearRepository(_env.Db, TestEnvironment.Logger<AcademicYearRepository>()),
            new FeeHeadRepository(_env.Db, TestEnvironment.Logger<FeeHeadRepository>()));
    }

    private InvoiceRequest Invoice(decimal total) => new()
    {
        StudentId = _student.Id, ClassId = _class.Id, BranchId = _class.BranchId, InvoiceDate = new DateOnly(2026, 7, 1), TotalAmount = total,
        PaidAmount = 5000, RemainingAmount = 1, Status = "Paid", // must be ignored
    };

    [Fact]
    public async Task A_manual_invoice_is_split_over_this_years_fee_heads_and_adds_up()
    {
        var created = await _invoices.CreateAsync(Invoice(10000.01m));

        var items = _env.Db.InvoiceItems.Where(i => i.InvoiceId == created.Id).ToList();
        Assert.Equal(2, items.Count);                                   // last year's 99 999 head price is not used
        Assert.Equal(10000.01m, items.Sum(i => i.TotalAmount));         // rounding never loses a paisa
        Assert.Equal(7500.01m, items.Single(i => i.FeeHeadId == _tuition.Id).TotalAmount); // 3/4 of 10 000.01, rounded
        var invoice = (await _env.Db.Invoices.FindAsync(created.Id))!;
        Assert.Equal((0m, 10000.01m, InvoiceStatuses.Open), (invoice.PaidAmount!.Value, invoice.RemainingAmount!.Value, invoice.Status));
    }

    [Fact]
    public async Task Amounts_cannot_be_edited_after_a_payment_and_a_paid_invoice_cannot_be_deleted()
    {
        var created = await _invoices.CreateAsync(Invoice(4000));
        var item = _env.Db.InvoiceItems.First(i => i.InvoiceId == created.Id);
        await _payments.CreateAsync(new PaymentRequest
        {
            StudentId = _student.Id, PaymentDate = new DateOnly(2026, 11, 3), PaymentMethod = "Cash",
            FeeLines = new[] { new PaymentFeeLineRequest { InvoiceItemId = item.Id, Amount = 1000 } },
        });

        await ErrorAssert.ConflictAsync(() => _invoices.UpdateAsync(created.Id, Invoice(9000)));
        var renamed = Invoice(4000);
        renamed.InvoiceNo = "INV-42";
        var updated = await _invoices.UpdateAsync(created.Id, renamed);
        Assert.Equal(("INV-42", 1000m, InvoiceStatuses.PartiallyPaid), (updated.InvoiceNo, updated.PaidAmount!.Value, updated.Status));

        await ErrorAssert.ConflictAsync(() => _invoices.DeleteAsync(created.Id));
    }

    [Fact]
    public async Task An_unpaid_invoice_is_deleted_with_its_items()
    {
        var created = await _invoices.CreateAsync(Invoice(4000));

        await _invoices.DeleteAsync(created.Id);

        Assert.Empty(_env.Db.Invoices);
        Assert.Empty(_env.Db.InvoiceItems);
    }

    [Fact]
    public async Task A_class_fee_is_priced_once_per_year_head_and_installment_with_the_year_label_from_the_year()
    {
        var request = new ClassFeeMapRequest { ClassId = _class.Id, AcademicYearId = _year.Id, FeeHeadId = _tuition.Id, Amount = 1000, InstallmentNo = 2, AcademicYear = "wrong" };
        var created = await _feeMaps.CreateAsync(request);
        Assert.Equal("2026-27", created.AcademicYear);

        await ErrorAssert.ConflictAsync(() => _feeMaps.CreateAsync(request));
        await ErrorAssert.BadRequestAsync(() => _feeMaps.CreateAsync(new ClassFeeMapRequest { ClassId = _class.Id, AcademicYearId = Guid.NewGuid(), FeeHeadId = _tuition.Id, Amount = 1 }));
    }

    public void Dispose() => _env.Dispose();
}
