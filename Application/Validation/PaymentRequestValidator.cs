using System;
using FluentValidation;
using Application.ViewModels.Payment;

namespace Application.Validation.Payment
{
    /// <summary>
    /// Payment form rules. Amount / fee-line consistency, the student and fee heads are checked by PaymentService.
    /// Every non-cash payment needs its reference (cheque, UPI or bank transaction number) so it can be reconciled.
    /// </summary>
    public class PaymentRequestValidator : AbstractValidator<PaymentRequest>
    {
        public PaymentRequestValidator()
        {
            RuleFor(x => x.StudentId).NotNull().WithMessage("StudentId is required.");
            RuleFor(x => x.PaymentMethod).NotEmpty().WithMessage("PaymentMethod is required.").MaximumLength(50);
            RuleFor(x => x.PaymentDate).NotNull().WithMessage("PaymentDate is required.");
            RuleFor(x => x.Amount).GreaterThan(0).When(x => x.Amount.HasValue);
            RuleFor(x => x.Amount).NotNull().WithMessage("Enter the amount or the fee lines.")
                .When(x => x.FeeLines == null || x.FeeLines.Length == 0);
            RuleFor(x => x.TransactionNumber)
                .NotEmpty().WithMessage("A transaction / cheque number is required for non-cash payments.")
                .When(x => !string.IsNullOrWhiteSpace(x.PaymentMethod) && !x.PaymentMethod.Trim().Equals("cash", StringComparison.OrdinalIgnoreCase));
            RuleFor(x => x.TransactionNumber).MaximumLength(100);
            RuleFor(x => x.BankName).MaximumLength(100);
            RuleForEach(x => x.FeeLines).ChildRules(line =>
            {
                line.RuleFor(l => l.Amount).NotNull().GreaterThan(0);
                line.RuleFor(l => l.CustomFeeName).MaximumLength(200);
            });
        }
    }
}
