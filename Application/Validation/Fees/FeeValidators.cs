using Application.Validation.Common;
using Application.ViewModels.FeeHead;
using Application.ViewModels.FeeInstallment;
using Application.ViewModels.FeeStructure;
using FluentValidation;

namespace Application.Validation.Fees
{
    public class FeeHeadRequestValidator : AbstractValidator<FeeHeadRequest>
    {
        public FeeHeadRequestValidator()
        {
            RuleFor(x => x.Name).RequiredName(100);
            RuleFor(x => x.AccountCode).MaximumLength(50);
        }
    }

    public class FeeStructureRequestValidator : AbstractValidator<FeeStructureRequest>
    {
        public FeeStructureRequestValidator()
        {
            RuleFor(x => x.ClassId).NotNull();
            RuleFor(x => x.FeeHeadId).NotNull();
            RuleFor(x => x.Amount).NotNull().GreaterThan(0);
        }
    }

    public class ClassFeeMapRequestValidator : AbstractValidator<Application.ViewModels.ClassFeeMap.ClassFeeMapRequest>
    {
        public ClassFeeMapRequestValidator()
        {
            RuleFor(x => x.ClassId).NotNull();
            RuleFor(x => x.AcademicYearId).NotNull();
            RuleFor(x => x.FeeHeadId).NotNull();
            RuleFor(x => x.Amount).NotNull().GreaterThan(0);
            RuleFor(x => x.InstallmentNo).InclusiveBetween(1, 24).When(x => x.InstallmentNo.HasValue);
        }
    }

    /// <summary>Installments are numbered from 1, need a due date, and late fees start no earlier than the due date.</summary>
    public class FeeInstallmentRequestValidator : AbstractValidator<FeeInstallmentRequest>
    {
        public FeeInstallmentRequestValidator()
        {
            RuleFor(x => x.ClassId).NotNull();
            RuleFor(x => x.AcademicYearId).NotNull();
            RuleFor(x => x.InstallmentNo).NotNull().InclusiveBetween(1, 24);
            RuleFor(x => x.InstallmentName).MaximumLength(100);
            RuleFor(x => x.DueDate).NotNull();
            RuleFor(x => x.LateFeeAfter)
                .GreaterThanOrEqualTo(x => x.DueDate!.Value).WithMessage("Late fees cannot start before the due date.")
                .When(x => x.DueDate.HasValue && x.LateFeeAfter.HasValue);
        }
    }
}
