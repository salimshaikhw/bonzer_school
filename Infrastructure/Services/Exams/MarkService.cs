using Application.Abstractions;
using Application.Abstractions.Services.Exams;
using Application.Students;
using Application.ViewModels.Mark;
using AutoMapper;
using Infrastructure.Services.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Exams
{
    /// <summary>
    /// Marks of one student for one exam component. Rules:
    ///   - the exam is not locked;
    ///   - the student exists, is on the roll, and (for an exam of the student's current academic year) is in
    ///     the exam's class and division;
    ///   - absent / medical / exempt marks carry no marks; otherwise 0 &lt;= marks &lt;= the component's maximum;
    ///   - one mark per student and component (also a unique index); the entry time is recorded.
    /// </summary>
    public class MarkService : CrudService<Mark, MarkRequest, MarkResponse>, IMarkService
    {
        private readonly ExamStructure _exams;
        private readonly IStudentRepository _students;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TimeProvider _clock;

        public MarkService(ILogger<MarkService> logger, IMarkRepository repository, IUnitOfWork unitOfWork, IMapper mapper,
            ExamStructure exams, IStudentRepository students, TimeProvider clock)
            : base(repository, mapper, logger)
        {
            _exams = exams;
            _students = students;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        protected override string EntityName => "Mark";

        public async Task<IEnumerable<MarkResponse>> GetByStudentIdAsync(Guid studentId)
        {
            var rows = await Repository.FindAllAsync(m => m.StudentId == studentId);
            return Mapper.Map<IEnumerable<MarkResponse>>(rows.OrderByDescending(m => m.EnteredAt).ThenBy(m => m.ExamSubjectComponentId));
        }

        /// <summary>Kept for API compatibility; every change is already saved.</summary>
        public Task<int> CommitAsync() => _unitOfWork.Commit();

        protected override Task ValidateCreateAsync(MarkRequest request) => ValidateAsync(request, exceptId: null);

        protected override Task ValidateUpdateAsync(Mark existing, MarkRequest request)
        {
            if (request.StudentId != existing.StudentId || request.ExamSubjectComponentId != existing.ExamSubjectComponentId)
                throw RuleViolation("The student and component of a mark cannot be changed; delete it and enter the right one.");
            return ValidateAsync(request, existing.Id);
        }

        protected override async Task ValidateDeleteAsync(Mark existing)
        {
            var (_, _, exam) = await _exams.GetComponentAsync(existing.ExamSubjectComponentId);
            ExamStructure.EnsureUnlocked(exam);
        }

        protected override void BeforeSave(Mark entity)
        {
            if (entity.IsAbsent == true || entity.IsMedical == true || entity.IsExempt == true)
                entity.MarksObtained = null;
            entity.EnteredAt = _clock.GetUtcNow().UtcDateTime;
        }

        private async Task ValidateAsync(MarkRequest request, Guid? exceptId)
        {
            var (component, _, exam) = await _exams.GetComponentAsync(request.ExamSubjectComponentId);
            ExamStructure.EnsureUnlocked(exam);

            if (request.StudentId is not { } studentId)
                throw RuleViolation("A student is required.");
            var student = await _students.FindAsync(s => s.Id == studentId) ?? throw RuleViolation($"Student {studentId} does not exist.");
            ExamStructure.EnsureStudentSitsExam(student, exam);

            var notMarked = request.IsAbsent == true || request.IsMedical == true || request.IsExempt == true;
            if (!notMarked)
            {
                if (request.MarksObtained is not { } marks)
                    throw RuleViolation("Enter the marks, or mark the student absent, medical or exempt.");
                if (marks < 0 || (component.MaxMarks is { } max && marks > max))
                    throw RuleViolation($"Marks must be between 0 and {component.MaxMarks}.");
            }
            else if (request.MarksObtained is > 0)
            {
                throw RuleViolation("An absent, medical or exempt student cannot also have marks.");
            }

            if (await Repository.IsExistAsync(m => m.StudentId == studentId && m.ExamSubjectComponentId == component.Id && m.Id != exceptId))
                throw Conflict("Marks for this student and component are already entered; update them instead.");
        }
    }
}
