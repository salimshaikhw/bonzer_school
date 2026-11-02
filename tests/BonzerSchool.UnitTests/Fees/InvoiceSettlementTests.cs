using Application.Abstractions;
using Application.Fees;
using Application.ViewModels.Payment;
using BonzerSchool.UnitTests.TestSupport;
using Domain.Entities.Fees;
using Domain.Entities.Students;
using Infrastructure.Persistence.Repositories.Fees;
using Infrastructure.Services.Fees;

namespace BonzerSchool.UnitTests.Fees;

public class InvoiceSettlementTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private readonly PaymentService _payments;
    private readonly Student _student;
    private readonly Invoice _invoice;
    private readonly InvoiceItem _tuition;   // 10 000
    private readonly InvoiceItem _transport; //  2 000

    public InvoiceSettlementTests()
    {
        var branch = _env.AddBranch();
        _student = new Student { Id = Guid.NewGuid(), FirstName = "Riya", BranchId = branch.Id };
        var head = new FeeHead { Id = Guid.NewGuid(), Name = "Tuition" };
        _invoice = new Invoice { Id = Guid.NewGuid(), StudentId = _student.Id, BranchId = branch.Id, TotalAmount = 12000, PaidAmount = 0, RemainingAmount = 12000, Status = InvoiceStatuses.Open };
        _tuition = new InvoiceItem { Id = Guid.NewGuid(), InvoiceId = _invoice.Id, FeeHeadId = head.Id, TotalAmount = 10000, RemainingAmount = 10000 };
        _transport = new InvoiceItem { Id = Guid.NewGuid(), InvoiceId = _invoice.Id, FeeHeadId = head.Id, TotalAmount = 2000, RemainingAmount = 2000 };
        _env.Db.AddRange(_student, head, _invoice, _tuition, _transport);
        _env.Db.SaveChanges();
        _payments = new PaymentService(TestEnvironment.Logger<PaymentService>(), new PaymentRepository(_env.Db, TestEnvironment.Logger<PaymentRepository>()),
            Substitute.For<IUnitOfWork>(), _env.Mapper, _env.Db, new FixedClock(new DateTimeOffset(2026, 11, 3, 9, 0, 0, TimeSpan.Zero)));
    }

    private PaymentRequest Pay(Guid studentId, params (InvoiceItem item, decimal amount)[] lines) => new()
    {
        StudentId = studentId, PaymentDate = new DateOnly(2026, 11, 3), PaymentMethod = "Cash",
        FeeLines = lines.Select(l => new PaymentFeeLineRequest { InvoiceItemId = l.item.Id, Amount = l.amount }).ToArray(),
    };

    private (InvoiceItem Tuition, Invoice Invoice) Reload()
    {
        _env.Db.ChangeTracker.Clear();
        return (_env.Db.InvoiceItems.Find(_tuition.Id)!, _env.Db.Invoices.Find(_invoice.Id)!);
    }

    [Fact]
    public async Task A_payment_reduces_the_invoice_balance()
    {
        await _payments.CreateAsync(Pay(_student.Id, (_tuition, 4000)));

        var (tuition, invoice) = Reload();
        Assert.Equal(6000, tuition.RemainingAmount);
        Assert.Equal(4000, invoice.PaidAmount);
        Assert.Equal(8000, invoice.RemainingAmount);
        Assert.Equal(InvoiceStatuses.PartiallyPaid, invoice.Status);
        Assert.Equal(tuition.FeeHeadId, _env.Db.PaymentFeeLines.Single().FeeHeadId); // taken from the item
    }

    [Fact]
    public async Task An_invoice_cannot_be_paid_twice()
    {
        await _payments.CreateAsync(Pay(_student.Id, (_tuition, 10000), (_transport, 2000)));
        Assert.Equal(InvoiceStatuses.Paid, Reload().Invoice.Status);

        await ErrorAssert.ConflictAsync(() => _payments.CreateAsync(Pay(_student.Id, (_tuition, 10000))));
        Assert.Single(_env.Db.Payments); // the second payment saved nothing
    }

    [Fact]
    public async Task Deleting_the_payment_restores_the_balance()
    {
        var paid = await _payments.CreateAsync(Pay(_student.Id, (_tuition, 4000)));

        await _payments.DeleteAsync(paid.Id);

        var (tuition, invoice) = Reload();
        Assert.Equal(10000, tuition.RemainingAmount);
        Assert.Equal(InvoiceStatuses.Open, invoice.Status);
    }

    [Fact]
    public async Task Another_students_invoice_cannot_be_paid()
    {
        var other = new Student { Id = Guid.NewGuid(), FirstName = "Ravi", BranchId = _student.BranchId };
        _env.Db.Students.Add(other);
        await _env.Db.SaveChangesAsync();

        await ErrorAssert.BadRequestAsync(() => _payments.CreateAsync(Pay(other.Id, (_tuition, 100))));
        Assert.Equal(10000, Reload().Tuition.RemainingAmount);
    }

    public void Dispose() => _env.Dispose();
}
