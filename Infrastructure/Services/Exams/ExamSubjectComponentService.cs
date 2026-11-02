using Application.Abstractions.Services.Exams;
using Application.ViewModels.ExamSubjectComponent;
using AutoMapper;
using Infrastructure.Services.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Exams
{
    /// <summary>
    /// Components of an exam subject (theory 80 + practical 20, ...). Rules: the exam is not locked; the component
    /// type exists and appears once per subject; the components' maximum marks add up to no more than the
    /// subject's TotalMaxMarks (when set).
    /// </summary>
    public class ExamSubjectComponentService : CrudService<ExamSubjectComponent, ExamSubjectComponentRequest, ExamSubjectComponentResponse>, IExamSubjectComponentService
    {
        private readonly ExamStructure _exams;
        private readonly IExamComponentTypeRepository _componentTypes;

        public ExamSubjectComponentService(ILogger<ExamSubjectComponentService> logger, IExamSubjectComponentRepository repository, IMapper mapper,
            ExamStructure exams, IExamComponentTypeRepository componentTypes)
            : base(repository, mapper, logger)
        {
            _exams = exams;
            _componentTypes = componentTypes;
        }

        protected override string EntityName => "ExamSubjectComponent";

        protected override Task ValidateCreateAsync(ExamSubjectComponentRequest request) => ValidateAsync(request, exceptId: null);

        protected override Task ValidateUpdateAsync(ExamSubjectComponent existing, ExamSubjectComponentRequest request) => ValidateAsync(request, existing.Id);

        protected override async Task ValidateDeleteAsync(ExamSubjectComponent existing)
        {
            var (_, exam) = await _exams.GetExamSubjectAsync(existing.ExamSubjectId);
            ExamStructure.EnsureUnlocked(exam);
        }

        private async Task ValidateAsync(ExamSubjectComponentRequest request, Guid? exceptId)
        {
            var (subject, exam) = await _exams.GetExamSubjectAsync(request.ExamSubjectId);
            ExamStructure.EnsureUnlocked(exam);

            if (request.ComponentTypeId is { } typeId && !await _componentTypes.IsExistAsync(t => t.Id == typeId))
                throw RuleViolation($"Component type {typeId} does not exist.");
            if (await Repository.IsExistAsync(c => c.ExamSubjectId == subject.Id && c.ComponentTypeId == request.ComponentTypeId && c.Id != exceptId))
                throw Conflict("This component type is already part of the subject.");

            if (subject.TotalMaxMarks is { } subjectMax && request.MaxMarks is { } max)
            {
                var others = await Repository.FindAllAsync(c => c.ExamSubjectId == subject.Id && c.Id != exceptId);
                var total = others.Sum(c => c.MaxMarks ?? 0) + max;
                if (total > subjectMax)
                    throw RuleViolation($"The components would total {total} marks, more than the subject's maximum of {subjectMax}.");
            }
        }
    }
}
