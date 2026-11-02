using Application.Common;
using Application.Exceptions;
using Application.Students;
using Application.Validation.Common;
using Domain.Entities.Students;

namespace BonzerSchool.UnitTests.Students;

public class StudentLifecycleTests
{
    private static readonly DateOnly Today = new(2026, 11, 3);

    private static Student InStatus(string? status, bool tcIssued = false) => new()
    {
        Id = Guid.NewGuid(), StudentStatus = status, IsActive = status is null or StudentStatuses.Active or StudentStatuses.Suspended,
        IsTcIssued = tcIssued, ClassId = Guid.NewGuid(),
    };

    // Every (status, action) pair: allowed or not, and the resulting status.
    [Theory]
    [InlineData(StudentStatuses.Active, StudentAction.Suspend, StudentStatuses.Suspended)]
    [InlineData(StudentStatuses.Suspended, StudentAction.Activate, StudentStatuses.Active)]
    [InlineData(StudentStatuses.Active, StudentAction.Drop, StudentStatuses.Dropped)]
    [InlineData(StudentStatuses.Suspended, StudentAction.Drop, StudentStatuses.Dropped)]
    [InlineData(StudentStatuses.Active, StudentAction.PassOut, StudentStatuses.PassedOut)]
    [InlineData(StudentStatuses.Active, StudentAction.IssueTc, StudentStatuses.Transferred)]
    [InlineData(StudentStatuses.PassedOut, StudentAction.IssueTc, StudentStatuses.Transferred)]
    [InlineData(StudentStatuses.Dropped, StudentAction.IssueTc, StudentStatuses.Transferred)]
    [InlineData(StudentStatuses.Dropped, StudentAction.ReEnroll, StudentStatuses.Active)]
    [InlineData(StudentStatuses.Transferred, StudentAction.ReEnroll, StudentStatuses.Active)]
    [InlineData(StudentStatuses.Active, StudentAction.Promote, StudentStatuses.Active)]
    [InlineData(StudentStatuses.Active, StudentAction.TransferBranch, StudentStatuses.Active)]
    [InlineData(null, StudentAction.Suspend, StudentStatuses.Suspended)] // legacy rows without status count as active
    [InlineData("active", StudentAction.Drop, StudentStatuses.Dropped)]   // case-insensitive
    public void Allowed_transitions_change_the_status(string? from, StudentAction action, string expected)
    {
        var student = InStatus(from);

        StudentLifecycle.Apply(student, action, Today);

        Assert.Equal(expected, student.StudentStatus);
    }

    [Theory]
    [InlineData(StudentStatuses.Dropped, StudentAction.Promote)]
    [InlineData(StudentStatuses.Suspended, StudentAction.Promote)]
    [InlineData(StudentStatuses.PassedOut, StudentAction.Suspend)]
    [InlineData(StudentStatuses.Transferred, StudentAction.Suspend)]
    [InlineData(StudentStatuses.Active, StudentAction.Activate)]
    [InlineData(StudentStatuses.Active, StudentAction.ReEnroll)]
    [InlineData(StudentStatuses.PassedOut, StudentAction.ReEnroll)]
    [InlineData(StudentStatuses.Dropped, StudentAction.TransferBranch)]
    [InlineData(StudentStatuses.Transferred, StudentAction.IssueTc)]
    public void Disallowed_transitions_are_refused_with_a_409(string from, StudentAction action)
    {
        var student = InStatus(from);

        var error = Assert.Throws<BaseServiceException>(() => StudentLifecycle.Apply(student, action, Today));

        Assert.Equal(ExceptionCodes.Operation, error.ExceptionCode);
        Assert.Equal(from, student.StudentStatus); // unchanged
        Assert.Contains(from, error.Message);
    }

    [Theory]
    [InlineData(StudentAction.Drop)]
    [InlineData(StudentAction.PassOut)]
    [InlineData(StudentAction.IssueTc)]
    public void Leaving_deactivates_and_records_the_leaving_date(StudentAction action)
    {
        var student = InStatus(StudentStatuses.Active);

        StudentLifecycle.Apply(student, action, Today);

        Assert.False(student.IsActive);
        Assert.Equal(Today, student.LeavingDate);
    }

    [Fact]
    public void An_existing_leaving_date_is_kept()
    {
        var student = InStatus(StudentStatuses.Active);
        student.LeavingDate = new DateOnly(2026, 10, 1);

        StudentLifecycle.Apply(student, StudentAction.Drop, Today);

        Assert.Equal(new DateOnly(2026, 10, 1), student.LeavingDate);
    }

    [Fact]
    public void A_tc_is_issued_only_once_and_re_enrolment_starts_a_new_period()
    {
        var student = InStatus(StudentStatuses.Active);
        StudentLifecycle.Apply(student, StudentAction.IssueTc, Today);
        Assert.True(student.IsTcIssued);

        StudentLifecycle.Apply(student, StudentAction.ReEnroll, Today);

        Assert.Equal(StudentStatuses.Active, student.StudentStatus);
        Assert.True(student.IsActive);
        Assert.Null(student.LeavingDate);
        Assert.False(student.IsTcIssued);
    }

    [Fact]
    public void A_student_with_a_tc_already_cannot_get_another()
    {
        var student = InStatus(StudentStatuses.PassedOut, tcIssued: true);

        Assert.Contains("already been issued", StudentLifecycle.WhyNot(student, StudentAction.IssueTc));
    }

    [Fact]
    public void Promotion_needs_a_current_class()
    {
        var student = InStatus(StudentStatuses.Active);
        student.ClassId = null;

        Assert.False(StudentLifecycle.Allows(student, StudentAction.Promote));
    }

    [Theory]
    [InlineData(StudentStatuses.Suspended, "active", StudentAction.Activate)]
    [InlineData(StudentStatuses.Dropped, "ACTIVE", StudentAction.ReEnroll)]
    [InlineData(StudentStatuses.Active, "suspended", StudentAction.Suspend)]
    [InlineData(StudentStatuses.Active, "PASSED_OUT", StudentAction.PassOut)]
    [InlineData(StudentStatuses.Active, "TRANSFERRED", StudentAction.IssueTc)]
    public void A_target_status_maps_to_its_action(string from, string target, StudentAction expected)
    {
        Assert.Equal(expected, StudentLifecycle.ActionFor(InStatus(from), target));
    }

    [Fact]
    public void Unknown_target_status_is_a_400()
    {
        var error = Assert.Throws<BaseServiceException>(() => StudentLifecycle.ActionFor(InStatus(StudentStatuses.Active), "GRADUATED"));
        Assert.Equal(ExceptionCodes.Validation, error.ExceptionCode);
    }
}

public class AadhaarTests
{
    [Theory]
    [InlineData("234567890124")]
    [InlineData("4987 6543 2102")]
    [InlineData("5678-1234-0982")]
    public void Valid_numbers_pass(string number) => Assert.True(Aadhaar.IsValid(number), Aadhaar.Problem(number));

    [Theory]
    [InlineData("234567890125", "check digit")]   // last digit typo
    [InlineData("243567890124", "check digit")]   // two digits swapped
    [InlineData("12345678901", "12 digits")]
    [InlineData("1234567890123", "12 digits")]
    [InlineData("134567890124", "0 or 1")]
    [InlineData("23456789012A", "12 digits")]
    public void Invalid_numbers_say_why(string number, string reason) => Assert.Contains(reason, Aadhaar.Problem(number));

    [Fact]
    public void Numbers_are_stored_as_plain_digits() => Assert.Equal("498765432102", Aadhaar.Normalize(" 4987 6543-2102 "));
}
