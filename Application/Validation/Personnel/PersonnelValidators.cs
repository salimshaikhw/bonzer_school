using System;
using Application.Validation.Common;
using Application.ViewModels.Department;
using Application.ViewModels.Staff;
using Application.ViewModels.StaffRole;
using FluentValidation;

namespace Application.Validation.Personnel
{
    /// <summary>
    /// Staff data rules: a first name and branch are required; contact fields are well-formed; staff are adults
    /// (at least 18) when they join, not born in the future, and not hired more than a year ahead.
    /// </summary>
    public class StaffRequestValidator : AbstractValidator<StaffRequest>
    {
        public const int MinimumAgeAtJoining = 18;

        public StaffRequestValidator()
        {
            RuleFor(x => x.BranchId).NotNull();
            RuleFor(x => x.FirstName).RequiredName(100);
            RuleFor(x => x.MiddleName).MaximumLength(100);
            RuleFor(x => x.LastName).MaximumLength(100);
            RuleFor(x => x.StaffCode).MaximumLength(50);
            RuleFor(x => x.Designation).MaximumLength(100);
            RuleFor(x => x.Gender).MaximumLength(20);
            RuleFor(x => x.Phone).MaximumLength(20).PhoneNumber();
            RuleFor(x => x.Email).OptionalEmail();
            RuleFor(x => x.City).MaximumLength(100);
            RuleFor(x => x.State).MaximumLength(100);
            RuleFor(x => x.Qualification).MaximumLength(50);
            RuleFor(x => x.QualificationDetails).MaximumLength(200);
            RuleFor(x => x.PreviousEmployer).MaximumLength(200);
            RuleFor(x => x.Salary).GreaterThanOrEqualTo(0).When(x => x.Salary.HasValue);
            RuleFor(x => x.YearsOfExperience).InclusiveBetween(0, 60).When(x => x.YearsOfExperience.HasValue);

            RuleFor(x => x.Dob)
                .LessThan(_ => Today()).WithMessage("Date of birth must be in the past.")
                .When(x => x.Dob.HasValue);
            RuleFor(x => x.JoiningDate)
                .LessThanOrEqualTo(_ => Today().AddYears(1)).WithMessage("Joining date cannot be more than a year ahead.")
                .When(x => x.JoiningDate.HasValue);
            RuleFor(x => x.JoiningDate)
                .GreaterThanOrEqualTo(x => x.Dob!.Value.AddYears(MinimumAgeAtJoining))
                .WithMessage($"Staff must be at least {MinimumAgeAtJoining} years old on the joining date.")
                .When(x => x.Dob.HasValue && x.JoiningDate.HasValue);
        }

        private static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);
    }

    public class DepartmentRequestValidator : AbstractValidator<DepartmentRequest>
    {
        public DepartmentRequestValidator()
        {
            RuleFor(x => x.Name).RequiredName(20);
        }
    }

    public class StaffRoleRequestValidator : AbstractValidator<StaffRoleRequest>
    {
        public StaffRoleRequestValidator()
        {
            RuleFor(x => x.StaffId).NotNull();
            RuleFor(x => x.RoleId).NotNull();
        }
    }
}
