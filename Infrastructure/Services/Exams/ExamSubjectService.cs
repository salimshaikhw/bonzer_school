using Application.Abstractions.Services.Academics;
using Application.Abstractions.Services.Exams;
using Application.ViewModels.ExamSubject;
using AutoMapper;
using Infrastructure.Services.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Exams
{
    /// <summary>
    /// Subjects of an exam (paper date, time, max / pass marks). Rules: the exam is not locked; the subject exists
    /// and appears once per exam; the paper date lies within the exam's dates. Mark ranges are checked by the validator.
    /// </summary>
    public class ExamSubjectService : CrudService<ExamSubject, ExamSubjectRequest, ExamSubjectResponse>, IExamSubjectService
    {
        private readonly ExamStructure _exams;
        private readonly IAcademicStructure _structure;

        public ExamSubjectService(ILogger<ExamSubjectService> logger, IExamSubjectRepository repository, IMapper mapper,
            ExamStructure exams, IAcademicStructure structure)
            : base(repository, mapper, logger)
        {
            _exams = exams;
            _structure = structure;
        }

        protected override string EntityName => "ExamSubject";

        protected override Task ValidateCreateAsync(ExamSubjectRequest request) => ValidateAsync(request, exceptId: null);

        protected override Task ValidateUpdateAsync(ExamSubject existing, ExamSubjectRequest request) => ValidateAsync(request, existing.Id);

        protected override async Task ValidateDeleteAsync(ExamSubject existing) =>
            ExamStructure.EnsureUnlocked(await _exams.GetExamAsync(existing.ExamId));

        private async Task ValidateAsync(ExamSubjectRequest request, Guid? exceptId)
        {
            var exam = await _exams.GetExamAsync(request.ExamId);
            ExamStructure.EnsureUnlocked(exam);
            await _structure.EnsureSubjectExistsAsync(request.SubjectId);

            if (request.ExamDate is { } date
                && ((exam.StartDate is { } start && date < start) || (exam.EndDate is { } end && date > end)))
                throw RuleViolation($"The paper date must be within the exam ({exam.StartDate:dd-MM-yyyy} to {exam.EndDate:dd-MM-yyyy}).");

            if (await Repository.IsExistAsync(s => s.ExamId == exam.Id && s.SubjectId == request.SubjectId && s.Id != exceptId))
                throw Conflict("This subject is already part of the exam.");
        }
    }
}
