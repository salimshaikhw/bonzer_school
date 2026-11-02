using Application.Abstractions;
using Application.Validation.Payment;
using Application.ViewModels.FeeHead;
using Application.ViewModels.FeeInstallment;
using Application.ViewModels.FeeStructure;
using Application.ViewModels.Payment;
using BonzerSchool.UnitTests.TestSupport;
using Domain.Entities.Academics;
using Domain.Entities.Fees;
using Domain.Entities.Organization;
using Domain.Entities.Students;
using Infrastructure.Persistence.Repositories.Fees;
using Infrastructure.Persistence.Repositories.Organization;
using Infrastructure.Services.Fees;

namespace BonzerSchool.UnitTests.Fees;

public class PaymentServiceTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 11, 3);
    private readonly TestEnvironment _env = new();
    private readonly PaymentService _service;
    private readonly Student _student;
    private readonly FeeHead _tuition;
    private readonly FeeHead _transport;

    public PaymentServiceTests()
    {
        var branch = _env.AddBranch();
        var cls = _env.AddClass(branch.Id);
        var division = _env.AddDivision(cls);
        _student = new Student { Id = Guid.NewGuid(), FirstName = "Riya", BranchId = branch.Id, ClassId = cls.Id, DivisionId = division.Id };
        _tuition = new FeeHead { Id = Guid.NewGuid(), Name = "Tuition" };
        _transport = new FeeHead { Id = Guid.NewGuid(), Name = "Transport" };
        _env.Db.AddRange(_student, _tuition, _transport);
        _env.Db.SaveChanges();

        _service = new PaymentService(TestEnvironment.Logger<PaymentService>(), new PaymentRepository(_env.Db, TestEnvironment.Logger<PaymentRepository>()),
            Substitute.For<IUnitOfWork>(), _env.Mapper, _env.Db, new FixedClock(new DateTimeOffset(Today.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero)));
    }

    private PaymentRequest Payment(decimal? amount, params (Guid? head, decimal amount)[] lines) => new()
    {
        StudentId = _student.Id, PaymentDate = Today, PaymentMethod = "Cash", Amount = amount,
        FeeLines = lines.Length == 0 ? null : lines.Select(l => new PaymentFeeLineRequest { FeeHeadId = l.head, Amount = l.amount }).ToArray(),
    };

    [Fact]
    public async Task A_payment_takes_the_sum_of_its_lines_the_students_branch_and_a_receipt_number()
    {
        var paid = await _service.CreateAsync(Payment(null, (_tuition.Id, 5000), (_transport.Id, 1200)));

        var saved = (await _env.Db.Payments.FindAsync(paid.Id))!;
        Assert.Equal(6200, saved.Amount);
        Assert.Equal(_student.BranchId, saved.BranchId);
        Assert.Equal(_student.DivisionId, saved.DivisionId);
        Assert.Matches("^[0-9]{12}$", saved.InvoiceId);
        Assert.Equal(2, _env.Db.PaymentFeeLines.Count(l => l.PaymentId == paid.Id));
    }

    [Fact]
    public async Task A_bad_fee_line_saves_nothing()
    {
        await ErrorAssert.BadRequestAsync(() => _service.CreateAsync(Payment(null, (_tuition.Id, 5000), (_transport.Id, 0))));
        await ErrorAssert.BadRequestAsync(() => _service.CreateAsync(Payment(null, (Guid.NewGuid(), 100))));

        Assert.Empty(_env.Db.Payments);        // previously the payment was saved before the lines were checked
        Assert.Empty(_env.Db.PaymentFeeLines);
    }

    [Fact]
    public async Task The_stated_amount_must_match_the_lines_and_be_positive()
    {
        await ErrorAssert.BadRequestAsync(() => _service.CreateAsync(Payment(7000, (_tuition.Id, 5000))));
        await ErrorAssert.BadRequestAsync(() => _service.CreateAsync(Payment(0)));
    }

    [Fact]
    public async Task Unknown_students_and_future_dates_are_rejected()
    {
        var unknown = Payment(100);
        unknown.StudentId = Guid.NewGuid();
        await ErrorAssert.BadRequestAsync(() => _service.CreateAsync(unknown));

        var future = Payment(100);
        future.PaymentDate = Today.AddDays(10);
        await ErrorAssert.BadRequestAsync(() => _service.CreateAsync(future));
    }

    [Fact]
    public async Task Only_descriptive_fields_of_a_recorded_payment_can_change()
    {
        var paid = await _service.CreateAsync(Payment(500));

        await ErrorAssert.ConflictAsync(() => _service.UpdateAsync(paid.Id, new PaymentRequest { Amount = 50 }));
        await ErrorAssert.ConflictAsync(() => _service.UpdateAsync(paid.Id, new PaymentRequest { PaymentDate = Today.AddDays(-30) }));

        var corrected = await _service.UpdateAsync(paid.Id, new PaymentRequest { Remarks = "Paid by father", TransactionNumber = "UPI-123" });
        Assert.Equal("Paid by father", corrected.Remarks);
        Assert.Equal(500, (await _env.Db.Payments.FindAsync(paid.Id))!.Amount);
    }

    [Fact]
    public async Task Deleting_a_payment_removes_its_fee_lines_too()
    {
        var paid = await _service.CreateAsync(Payment(null, (_tuition.Id, 5000)));

        Assert.True(await _service.DeleteAsync(paid.Id));
        Assert.Empty(_env.Db.Payments);
        Assert.Empty(_env.Db.PaymentFeeLines);
    }

    public void Dispose() => _env.Dispose();
}

public class FeeSetupServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private readonly FeeHeadService _heads;
    private readonly FeeStructureService _structures;
    private readonly FeeInstallmentService _installments;
    private readonly Class _class;
    private readonly AcademicYear _year;

    public FeeSetupServiceTests()
    {
        _class = _env.AddClass(_env.AddBranch().Id);
        _year = new AcademicYear { Id = Guid.NewGuid(), YearName = "2026-27", StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2027, 4, 30) };
        _env.Db.AcademicYears.Add(_year);
        _env.Db.SaveChanges();

        var headRepository = new FeeHeadRepository(_env.Db, TestEnvironment.Logger<FeeHeadRepository>());
        _heads = new FeeHeadService(TestEnvironment.Logger<FeeHeadService>(), headRepository, _env.Mapper, _env.Cache, _env.Db);
        _structures = new FeeStructureService(TestEnvironment.Logger<FeeStructureService>(),
            new FeeStructureRepository(_env.Db, TestEnvironment.Logger<FeeStructureRepository>()), _env.Mapper, _env.AcademicStructure(), headRepository);
        _installments = new FeeInstallmentService(TestEnvironment.Logger<FeeInstallmentService>(),
            new FeeInstallmentRepository(_env.Db, TestEnvironment.Logger<FeeInstallmentRepository>()), _env.Mapper, _env.AcademicStructure(),
            new AcademicYearRepository(_env.Db, TestEnvironment.Logger<AcademicYearRepository>()));
    }

    [Fact]
    public async Task A_fee_head_is_priced_once_per_class_and_cannot_be_deleted_while_used()
    {
        var tuition = await _heads.CreateAsync(new FeeHeadRequest { Name = "Tuition", AccountCode = "fee-01" });
        await ErrorAssert.ConflictAsync(() => _heads.CreateAsync(new FeeHeadRequest { Name = "tuition" }));
        await ErrorAssert.ConflictAsync(() => _heads.CreateAsync(new FeeHeadRequest { Name = "Lab", AccountCode = "FEE-01" }));

        await _structures.CreateAsync(new FeeStructureRequest { ClassId = _class.Id, FeeHeadId = tuition.Id, Amount = 25000 });
        await ErrorAssert.ConflictAsync(() => _structures.CreateAsync(new FeeStructureRequest { ClassId = _class.Id, FeeHeadId = tuition.Id, Amount = 26000 }));
        await ErrorAssert.ConflictAsync(() => _heads.DeleteAsync(tuition.Id));
    }

    [Fact]
    public async Task Installments_are_numbered_once_and_due_within_the_academic_year()
    {
        var first = new FeeInstallmentRequest { ClassId = _class.Id, AcademicYearId = _year.Id, InstallmentNo = 1, DueDate = new DateOnly(2026, 6, 15) };
        var created = await _installments.CreateAsync(first);
        Assert.Equal(_class.BranchId, (await _env.Db.FeeInstallments.FindAsync(created.Id))!.BranchId);

        await ErrorAssert.ConflictAsync(() => _installments.CreateAsync(first));
        await ErrorAssert.BadRequestAsync(() => _installments.CreateAsync(
            new FeeInstallmentRequest { ClassId = _class.Id, AcademicYearId = _year.Id, InstallmentNo = 2, DueDate = new DateOnly(2027, 6, 1) }));
    }

    public void Dispose() => _env.Dispose();
}

public class PaymentRequestValidatorTests
{
    private readonly PaymentRequestValidator _validator = new();

    [Theory]
    [InlineData("Cash", null, true)]
    [InlineData("UPI", null, false)]
    [InlineData("Cheque", "004512", true)]
    [InlineData("cash ", null, true)]
    public void Non_cash_payments_need_a_reference(string method, string? reference, bool valid)
    {
        var request = new PaymentRequest { StudentId = Guid.NewGuid(), PaymentDate = new DateOnly(2026, 11, 3), Amount = 500, PaymentMethod = method, TransactionNumber = reference };

        Assert.Equal(valid, _validator.Validate(request).IsValid);
    }
}
