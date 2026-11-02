using BonzerSchool.UnitTests.TestSupport;
using Domain.Entities.Academics;
using Domain.Entities.Fees;
using Domain.Entities.Organization;
using Domain.Entities.Students;
using Infrastructure.Services.Fees;

namespace BonzerSchool.UnitTests.Fees;

/// <summary>
/// One branch, Class 5 with divisions A and B. Last year (2025-26) Class 4 fees were 20 000; this year (2026-27)
/// Class 5 fees are 30 000 for everyone plus 5 000 lab fee for division A only.
/// </summary>
public class RemainingFeesTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private readonly RemainingFeesService _service;
    private readonly Class _class5;
    private readonly Division _a;
    private readonly Division _b;
    private readonly AcademicYear _thisYear;
    private readonly Student _promoted; // in 5A, paid 20 000 last year and 10 000 this year
    private readonly Student _fresh;    // in 5B, paid nothing

    public RemainingFeesTests()
    {
        var branch = _env.AddBranch();
        var class4 = _env.AddClass(branch.Id, "Class 4");
        _class5 = _env.AddClass(branch.Id, "Class 5");
        _a = _env.AddDivision(_class5, "A");
        _b = _env.AddDivision(_class5, "B");
        var lastYear = new AcademicYear { Id = Guid.NewGuid(), YearName = "2025-26", StartDate = new DateOnly(2025, 6, 1), EndDate = new DateOnly(2026, 4, 30) };
        _thisYear = new AcademicYear { Id = Guid.NewGuid(), YearName = "2026-27", StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2027, 4, 30) };
        var head = new FeeHead { Id = Guid.NewGuid(), Name = "Tuition" };

        _promoted = new Student { Id = Guid.NewGuid(), FirstName = "Asha", BranchId = branch.Id, ClassId = _class5.Id, DivisionId = _a.Id, AcademicYearId = _thisYear.Id, IsActive = true };
        _fresh = new Student { Id = Guid.NewGuid(), FirstName = "Ravi", BranchId = branch.Id, ClassId = _class5.Id, DivisionId = _b.Id, AcademicYearId = _thisYear.Id, IsActive = true };

        _env.Db.AddRange(lastYear, _thisYear, head, _promoted, _fresh,
            new ClassFeeMap { Id = Guid.NewGuid(), ClassId = class4.Id, AcademicYearId = lastYear.Id, FeeHeadId = head.Id, Amount = 20000 },
            new ClassFeeMap { Id = Guid.NewGuid(), ClassId = _class5.Id, AcademicYearId = lastYear.Id, FeeHeadId = head.Id, Amount = 28000 }, // old price, other year
            new ClassFeeMap { Id = Guid.NewGuid(), ClassId = _class5.Id, AcademicYearId = _thisYear.Id, FeeHeadId = head.Id, Amount = 30000 },
            new ClassFeeMap { Id = Guid.NewGuid(), ClassId = _class5.Id, DivisionId = _a.Id, AcademicYearId = _thisYear.Id, FeeHeadId = head.Id, Amount = 5000 },
            new Payment { Id = Guid.NewGuid(), StudentId = _promoted.Id, BranchId = branch.Id, PaymentDate = new DateOnly(2025, 7, 1), Amount = 20000 },
            new Payment { Id = Guid.NewGuid(), StudentId = _promoted.Id, BranchId = branch.Id, PaymentDate = new DateOnly(2026, 7, 1), Amount = 10000 });
        _env.Db.SaveChanges();
        _service = new RemainingFeesService(_env.Db, TestEnvironment.Logger<RemainingFeesService>());
    }

    [Fact]
    public async Task Last_years_payments_do_not_reduce_this_years_fees()
    {
        var asha = await _service.GetStudentRemainingFeesAsync(_promoted.Id);

        Assert.Equal(35000, asha.TotalDueFees);  // 30 000 + division A lab fee
        Assert.Equal(10000, asha.PaidAmount);    // only this year's payment (previously 30 000)
        Assert.Equal(25000, asha.RemainingFees); // previously 5 000
    }

    [Fact]
    public async Task Division_fees_only_apply_to_that_division()
    {
        var ravi = await _service.GetStudentRemainingFeesAsync(_fresh.Id);

        Assert.Equal(30000, ravi.TotalDueFees);
    }

    [Fact]
    public async Task Class_division_and_branch_totals_are_the_sum_of_the_students()
    {
        var cls = await _service.GetClassRemainingFeesAsync(_class5.Id);
        Assert.Equal(65000, cls.TotalClassDueFees); // previously (28 000 + 30 000 + 5 000) x 2 = 126 000
        Assert.Equal(10000, cls.TotalPaidAmount);
        Assert.Equal(55000, cls.TotalRemainingFees);

        var divisionB = await _service.GetDivisionRemainingFeesAsync(_b.Id);
        Assert.Equal(30000, divisionB.TotalDivisionDueFees);

        var branch = await _service.GetBranchRemainingFeesAsync(_class5.BranchId!.Value);
        Assert.Equal(cls.TotalClassDueFees, branch.TotalBranchDueFees);
        Assert.Equal(cls.TotalRemainingFees, branch.TotalRemainingFees);
    }

    [Fact]
    public async Task Pending_lists_use_the_same_figures()
    {
        var pending = (await _service.GetPendingFeesStudentsByClassAsync(_class5.Id)).ToList();

        Assert.Equal(new[] { _fresh.Id, _promoted.Id }, pending.Select(p => p.StudentId)); // 30 000 owed, then 25 000
    }

    public void Dispose() => _env.Dispose();
}
