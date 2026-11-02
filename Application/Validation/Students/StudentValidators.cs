using System;
using Application.Students;
using Application.Validation.Common;
using Application.ViewModels.ParentUser;
using Application.ViewModels.Student;
using Application.ViewModels.StudentSubject;
using FluentValidation;

namespace Application.Validation.Students
{
    /// <summary>
    /// Admission form rules. Born in the past, admitted after birth with at least two years in between
    /// (play-group age) and not admitted more than a year ahead; Aadhaar valid when given; contact fields
    /// well-formed; status, when given, one of the known values.
    /// Uniqueness and branch / class / division consistency are checked by StudentService.
    /// </summary>
    public class StudentRequestValidator : AbstractValidator<StudentRequest>
    {
        public const int MinimumAgeAtAdmission = 2;

        public StudentRequestValidator()
        {
            RuleFor(x => x.BranchId).NotNull();
            RuleFor(x => x.FirstName).RequiredName(100);
            RuleFor(x => x.LastName).MaximumLength(100);
            RuleFor(x => x.AdmissionNo).MaximumLength(50);
            RuleFor(x => x.RollNumber).MaximumLength(50);
            RuleFor(x => x.Gender).MaximumLength(20);

            RuleFor(x => x.ChildAadhaar)
                .Must(value => Aadhaar.IsValid(value)).WithMessage(x => Aadhaar.Problem(x.ChildAadhaar)!)
                .When(x => !string.IsNullOrWhiteSpace(x.ChildAadhaar));

            RuleFor(x => x.Phone).PhoneNumber();
            RuleFor(x => x.FatherPhone).PhoneNumber();
            RuleFor(x => x.MotherPhone).PhoneNumber();
            RuleFor(x => x.GuardianPhone).PhoneNumber();
            RuleFor(x => x.Email).OptionalEmail();

            RuleFor(x => x.StudentStatus)
                .Must(StudentStatuses.IsKnown).WithMessage($"Status must be one of: {string.Join(", ", StudentStatuses.All)}.")
                .When(x => !string.IsNullOrWhiteSpace(x.StudentStatus));

            RuleFor(x => x.Dob)
                .LessThan(_ => Today()).WithMessage("Date of birth must be in the past.")
                .When(x => x.Dob.HasValue);
            RuleFor(x => x.AdmissionDate)
                .LessThanOrEqualTo(_ => Today().AddYears(1)).WithMessage("Admission date cannot be more than a year ahead.")
                .When(x => x.AdmissionDate.HasValue);
            RuleFor(x => x.AdmissionDate)
                .GreaterThanOrEqualTo(x => x.Dob!.Value.AddYears(MinimumAgeAtAdmission))
                .WithMessage($"A student must be at least {MinimumAgeAtAdmission} years old at admission.")
                .When(x => x.Dob.HasValue && x.AdmissionDate.HasValue);
        }

        private static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);
    }

    public class StudentSubjectRequestValidator : AbstractValidator<StudentSubjectRequest>
    {
        public StudentSubjectRequestValidator()
        {
            RuleFor(x => x.StudentId).NotNull();
            RuleFor(x => x.SubjectId).NotNull();
            RuleFor(x => x.AcademicYear).MaximumLength(20);
        }
    }

    public class ParentUserRequestValidator : AbstractValidator<ParentUserRequest>
    {
        public ParentUserRequestValidator()
        {
            RuleFor(x => x.StudentId).NotNull();
            RuleFor(x => x.Username).NotEmpty().OptionalEmail(100).WithMessage("Username must be the parent's login email.");
            RuleFor(x => x.Phone).MaximumLength(20).PhoneNumber();
        }
    }
}
