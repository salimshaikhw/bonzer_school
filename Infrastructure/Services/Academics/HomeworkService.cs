using Application.Abstractions.Services.Academics;
using Application.ViewModels.Homework;
using AutoMapper;
using Infrastructure.Services.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Academics
{
    /// <summary>
    /// Homework for a class and subject. Rules: class and subject exist; new homework is not due in the past
    /// (existing homework may keep a past due date when edited).
    /// </summary>
    public class HomeworkService : CrudService<Homework, HomeworkRequest, HomeworkResponse>, IHomeworkService
    {
        private readonly IAcademicStructure _structure;
        private readonly TimeProvider _clock;

        public HomeworkService(ILogger<HomeworkService> logger, IHomeworkRepository repository, IMapper mapper,
            IAcademicStructure structure, TimeProvider clock)
            : base(repository, mapper, logger)
        {
            _structure = structure;
            _clock = clock;
        }

        protected override string EntityName => "Homework";

        protected override async Task ValidateCreateAsync(HomeworkRequest request)
        {
            await ValidateReferencesAsync(request);
            // One day of slack: the server runs in UTC while schools may be up to 14 hours ahead.
            var earliest = DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime).AddDays(-1);
            if (request.DueDate < earliest)
                throw RuleViolation("The due date cannot be in the past.");
        }

        protected override Task ValidateUpdateAsync(Homework existing, HomeworkRequest request) => ValidateReferencesAsync(request);

        protected override void BeforeSave(Homework entity) => entity.Description = entity.Description?.Trim();

        private async Task ValidateReferencesAsync(HomeworkRequest request)
        {
            await _structure.GetClassAsync(request.ClassId);
            await _structure.EnsureSubjectExistsAsync(request.SubjectId);
        }
    }
}
