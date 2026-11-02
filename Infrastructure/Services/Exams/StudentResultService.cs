using Application.Abstractions.Services.Exams;
using Application.Exams;
using Application.ViewModels.StudentResult;
using AutoMapper;
using Infrastructure.Services.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Exams
{
    /// <summary>
    /// Exam results. Results are calculated from the marks with <see cref="GenerateResultsAsync"/>
    /// (ResultCalculator: totals, percentage, pass / fail, grade, rank); the CRUD methods remain for
    /// corrections, but not on a locked exam, and one result per student and exam.
    /// </summary>
    public class StudentResultService : CrudService<StudentResult, StudentResultRequest, StudentResultResponse>, IStudentResultService
    {
        private readonly ExamStructure _exams;
        private readonly IExamSubjectRepository _examSubjects;
        private readonly IExamSubjectComponentRepository _components;
        private readonly IMarkRepository _marks;
        private readonly IGradeMasterRepository _grades;
        private readonly TimeProvider _clock;

        public StudentResultService(ILogger<StudentResultService> logger, IStudentResultRepository repository, IMapper mapper,
            ExamStructure exams, IExamSubjectRepository examSubjects, IExamSubjectComponentRepository components,
            IMarkRepository marks, IGradeMasterRepository grades, TimeProvider clock)
            : base(repository, mapper, logger)
        {
            _exams = exams;
            _examSubjects = examSubjects;
            _components = components;
            _marks = marks;
            _grades = grades;
            _clock = clock;
        }

        protected override string EntityName => "StudentResult";

        /// <summary>
        /// (Re)calculates every student's result of the exam from the entered marks and saves them, replacing
        /// earlier results of that exam. Allowed on a locked exam: locking freezes the marks, then results are generated.
        /// </summary>
        public async Task<IReadOnlyList<StudentResultResponse>> GenerateResultsAsync(Guid examId)
        {
            var exam = await _exams.GetExamAsync(examId);
            var subjects = await _examSubjects.FindAllAsync(s => s.ExamId == exam.Id);
            if (subjects.Count == 0)
                throw RuleViolation("The exam has no subjects yet.");
            var subjectIds = subjects.Select(s => s.Id).ToList();
            var components = await _components.FindAllAsync(c => c.ExamSubjectId != null && subjectIds.Contains(c.ExamSubjectId.Value));
            var componentIds = components.Select(c => c.Id).ToList();
            var marks = await _marks.FindAllAsync(m => m.ExamSubjectComponentId != null && componentIds.Contains(m.ExamSubjectComponentId.Value));
            var grades = await _grades.GetAllAsync();

            var schemes = subjects.Select(s => new SubjectScheme(s.Id, s.TotalMaxMarks, s.TotalPassMarks,
                components.Where(c => c.ExamSubjectId == s.Id).Select(c => new ComponentScheme(c.Id, c.MaxMarks ?? 0, c.PassMarks)).ToList())).ToList();
            var outcomes = ResultCalculator.Calculate(
                schemes,
                marks.Where(m => m.StudentId != null && m.ExamSubjectComponentId != null)
                     .Select(m => new MarkEntry(m.StudentId!.Value, m.ExamSubjectComponentId!.Value, m.MarksObtained,
                         IsAbsent: m.IsAbsent == true || m.IsMedical == true, IsExempt: m.IsExempt == true)),
                grades.Where(g => g.Grade != null && g.MinPercent != null && g.MaxPercent != null)
                      .Select(g => new GradeBand(g.Grade!, g.MinPercent!.Value, g.MaxPercent!.Value)).ToList());

            var now = _clock.GetUtcNow().UtcDateTime;
            var existing = (await Repository.FindAllAsync(r => r.ExamId == exam.Id)).ToDictionary(r => r.StudentId ?? Guid.Empty);
            var saved = new List<StudentResult>();
            foreach (var outcome in outcomes)
            {
                var isNew = !existing.TryGetValue(outcome.StudentId, out var row);
                row ??= new StudentResult { Id = Guid.NewGuid(), ExamId = exam.Id, StudentId = outcome.StudentId };
                row.TotalMarks = outcome.TotalMarks;
                row.Percentage = outcome.Percentage;
                row.Result = outcome.Result;
                row.Grade = outcome.Grade;
                row.Rank = outcome.Rank;
                row.GeneratedAt = now;
                if (isNew) await Repository.AddAsync(row);
                else await Repository.UpdateAsync(row);
                saved.Add(row);
            }

            Logger.LogInformation("Generated {ResultCount} results for exam {ExamId}", saved.Count, exam.Id);
            return Mapper.Map<List<StudentResultResponse>>(saved);
        }

        protected override Task ValidateCreateAsync(StudentResultRequest request) => ValidateAsync(request, exceptId: null);

        protected override Task ValidateUpdateAsync(StudentResult existing, StudentResultRequest request) => ValidateAsync(request, existing.Id);

        protected override async Task ValidateDeleteAsync(StudentResult existing) =>
            ExamStructure.EnsureUnlocked(await _exams.GetExamAsync(existing.ExamId));

        private async Task ValidateAsync(StudentResultRequest request, Guid? exceptId)
        {
            ExamStructure.EnsureUnlocked(await _exams.GetExamAsync(request.ExamId));
            if (await Repository.IsExistAsync(r => r.ExamId == request.ExamId && r.StudentId == request.StudentId && r.Id != exceptId))
                throw Conflict("This student already has a result for the exam.");
        }
    }
}
