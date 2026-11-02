using Application.Abstractions.Services.Academics;
using Application.Abstractions.Services.Communication;
using Application.ViewModels.Notification;
using AutoMapper;
using Infrastructure.Services.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Communication
{
    /// <summary>
    /// Notifications to a student, a division, a branch, or (no target) the whole school group. Rules: the targets
    /// exist and fit together (the student is in the division, the division in the branch); the sent time is
    /// recorded by the server. Title and message are checked by NotificationRequestValidator.
    /// </summary>
    public class NotificationService : CrudService<Notification, NotificationRequest, NotificationResponse>, INotificationService
    {
        private readonly IAcademicStructure _structure;
        private readonly IDivisionRepository _divisions;
        private readonly IStudentRepository _students;
        private readonly TimeProvider _clock;

        public NotificationService(INotificationRepository repository, ILogger<NotificationService> logger, IMapper mapper,
            IAcademicStructure structure, IDivisionRepository divisions, IStudentRepository students, TimeProvider clock)
            : base(repository, mapper, logger)
        {
            _structure = structure;
            _divisions = divisions;
            _students = students;
            _clock = clock;
        }

        protected override string EntityName => "Notification";

        protected override Task ValidateCreateAsync(NotificationRequest request) => ValidateTargetsAsync(request);

        protected override Task ValidateUpdateAsync(Notification existing, NotificationRequest request) => ValidateTargetsAsync(request);

        protected override void BeforeSave(Notification entity)
        {
            entity.Title = entity.Title?.Trim();
            entity.SentAt ??= _clock.GetUtcNow().UtcDateTime;
        }

        private async Task ValidateTargetsAsync(NotificationRequest request)
        {
            if (request.BranchId != null)
                await _structure.EnsureBranchExistsAsync(request.BranchId);

            if (request.DivisionId is { } divisionId)
            {
                var division = await _divisions.FindAsync(d => d.Id == divisionId) ?? throw RuleViolation($"Division {divisionId} does not exist.");
                if (request.BranchId != null && division.BranchId != request.BranchId)
                    throw RuleViolation("The division is not part of the selected branch.");
            }

            if (request.StudentId is { } studentId)
            {
                var student = await _students.FindAsync(s => s.Id == studentId) ?? throw RuleViolation($"Student {studentId} does not exist.");
                if (request.DivisionId != null && student.DivisionId != request.DivisionId)
                    throw RuleViolation("The student is not in the selected division.");
                if (request.BranchId != null && student.BranchId != request.BranchId)
                    throw RuleViolation("The student is not in the selected branch.");
            }
        }
    }
}
