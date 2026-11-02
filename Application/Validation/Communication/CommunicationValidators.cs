using Application.Validation.Common;
using Application.ViewModels.AuditLog;
using Application.ViewModels.Notification;
using FluentValidation;

namespace Application.Validation.Communication
{
    public class NotificationRequestValidator : AbstractValidator<NotificationRequest>
    {
        public NotificationRequestValidator()
        {
            RuleFor(x => x.Title).RequiredName(200);
            RuleFor(x => x.Message).NotEmpty().MaximumLength(4000);
            RuleFor(x => x.Type).MaximumLength(50);
        }
    }

    public class AuditLogRequestValidator : AbstractValidator<AuditLogRequest>
    {
        public AuditLogRequestValidator()
        {
            RuleFor(x => x.Module).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ActionType).NotEmpty().MaximumLength(50);
        }
    }
}
