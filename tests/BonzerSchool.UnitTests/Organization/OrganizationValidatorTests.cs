using Application.Validation.Organization;
using Application.ViewModels.AcademicYear;
using Application.ViewModels.Branch;

namespace BonzerSchool.UnitTests.Organization;

public class OrganizationValidatorTests
{
    private readonly AcademicYearRequestValidator _yearValidator = new();
    private readonly BranchRequestValidator _branchValidator = new();

    [Fact]
    public void A_normal_academic_year_is_valid()
    {
        var result = _yearValidator.Validate(new AcademicYearRequest
        {
            YearName = "2026-27", StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2027, 4, 30)
        });

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Theory]
    [InlineData(null, "2026-06-01", "2027-04-30", "Year Name")]
    [InlineData("2026-27", "2026-06-01", "2026-05-31", "after the start")]
    [InlineData("2026-27", "2026-06-01", "2026-06-01", "after the start")]
    [InlineData("2026-29", "2026-06-01", "2028-06-02", "two years")]
    [InlineData("A name that is far too long", "2026-06-01", "2027-04-30", "Year Name")]
    public void Invalid_academic_years_are_rejected(string? name, string start, string end, string expectedMessagePart)
    {
        var result = _yearValidator.Validate(new AcademicYearRequest
        {
            YearName = name, StartDate = DateOnly.Parse(start), EndDate = DateOnly.Parse(end)
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains(expectedMessagePart));
    }

    [Theory]
    [InlineData("+91 98765 43210", true)]
    [InlineData("020-2612-3456", true)]
    [InlineData(null, true)]
    [InlineData("12345", false)]
    [InlineData("call me maybe", false)]
    public void Branch_phone_numbers_are_checked(string? phone, bool valid)
    {
        var result = _branchValidator.Validate(new BranchRequest { Name = "Main", Phone = phone });

        Assert.Equal(valid, result.IsValid);
    }

    [Theory]
    [InlineData("office@school.edu.in", true)]
    [InlineData("", true)]
    [InlineData("not-an-email", false)]
    [InlineData("office@localhost", false)]
    public void Branch_emails_are_checked(string email, bool valid)
    {
        var result = _branchValidator.Validate(new BranchRequest { Name = "Main", Email = email });

        Assert.Equal(valid, result.IsValid);
    }

    [Fact]
    public void Branch_name_is_required()
    {
        Assert.False(_branchValidator.Validate(new BranchRequest { Name = "   " }).IsValid);
    }
}
