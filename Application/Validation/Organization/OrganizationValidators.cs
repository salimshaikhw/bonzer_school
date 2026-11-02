using Application.Validation.Common;
using Application.ViewModels.AcademicYear;
using Application.ViewModels.Branch;
using Application.ViewModels.InstitutionType;
using Application.ViewModels.Medium;
using Application.ViewModels.SchoolGroup;
using FluentValidation;

namespace Application.Validation.Organization
{
    /// <summary>
    /// An academic year needs a name and both dates; it ends after it starts and spans at most two years
    /// (schools and universities run 10-12 months; two years leaves room for long coaching batches).
    /// </summary>
    public class AcademicYearRequestValidator : AbstractValidator<AcademicYearRequest>
    {
        public AcademicYearRequestValidator()
        {
            RuleFor(x => x.YearName).RequiredName(20);
            RuleFor(x => x.StartDate).NotNull();
            RuleFor(x => x.EndDate).NotNull();
            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate).WithMessage("End date must be after the start date.")
                .LessThanOrEqualTo(x => x.StartDate!.Value.AddYears(2)).WithMessage("An academic year can span at most two years.")
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue);
        }
    }

    public class BranchRequestValidator : AbstractValidator<BranchRequest>
    {
        public BranchRequestValidator()
        {
            RuleFor(x => x.Name).RequiredName(200);
            RuleFor(x => x.Schooluid).MaximumLength(50);
            RuleFor(x => x.Address).MaximumLength(500);
            RuleFor(x => x.City).MaximumLength(100);
            RuleFor(x => x.State).MaximumLength(100);
            RuleFor(x => x.Phone).PhoneNumber();
            RuleFor(x => x.Email).OptionalEmail();
        }
    }

    public class MediumRequestValidator : AbstractValidator<MediumRequest>
    {
        public MediumRequestValidator()
        {
            RuleFor(x => x.Name).RequiredName(50);
        }
    }

    public class InstitutionTypeRequestValidator : AbstractValidator<InstitutionTypeRequest>
    {
        public InstitutionTypeRequestValidator()
        {
            RuleFor(x => x.Name).RequiredName(50);
        }
    }

    public class UpdateSchoolGroupInputValidator : AbstractValidator<UpdateSchoolGroupInput>
    {
        public UpdateSchoolGroupInputValidator()
        {
            RuleFor(x => x.Name).RequiredName(200);
            RuleFor(x => x.Code).MaximumLength(50);
            RuleFor(x => x.Phone).MaximumLength(20).PhoneNumber();
            RuleFor(x => x.Email).OptionalEmail();
        }
    }
}
