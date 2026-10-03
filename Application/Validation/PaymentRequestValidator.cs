using FluentValidation;
using Application.ViewModels.Payment;

namespace Application.Validation.Payment
{
    public class PaymentRequestValidator : AbstractValidator<PaymentRequest>
    {
        public PaymentRequestValidator()
        {
            //RuleFor(x => x.InvoiceId).NotNull().WithMessage("InvoiceId is required.");
            RuleFor(x => x.StudentId).NotNull().WithMessage("StudentId is required.");
            //RuleFor(x => x.Amount).NotNull().GreaterThan(0).WithMessage("Amount must be greater than zero.");
            RuleFor(x => x.PaymentMethod).NotEmpty().WithMessage("PaymentMethod is required.");
            RuleFor(x => x.PaymentDate).NotNull().WithMessage("PaymentDate is required.");
            // Add more rules as needed for your business logic
        }
    }
}
