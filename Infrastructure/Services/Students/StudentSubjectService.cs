using Application.Abstractions.Services.Academics;
using Application.Abstractions.Services.Students;
using Application.ViewModels.StudentSubject;
using AutoMapper;
using Infrastructure.Services.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Students
{
    /// <summary>
    /// Subjects a student takes (electives, optional languages). Rules: student and subject exist; a subject is
    /// taken once per academic year.
    /// </summary>
    public class StudentSubjectService : CrudService<StudentSubject, StudentSubjectRequest, StudentSubjectResponse>, IStudentSubjectService
    {
        private readonly IStudentRepository _students;
        private readonly IAcademicStructure _structure;

        public StudentSubjectService(ILogger<StudentSubjectService> logger, IStudentSubjectRepository repository, IMapper mapper,
            IStudentRepository students, IAcademicStructure structure)
            : base(repository, mapper, logger)
        {
            _students = students;
            _structure = structure;
        }

        protected override string EntityName => "StudentSubject";

        protected override Task ValidateCreateAsync(StudentSubjectRequest request) => ValidateAsync(request, exceptId: null);

        protected override Task ValidateUpdateAsync(StudentSubject existing, StudentSubjectRequest request) => ValidateAsync(request, existing.Id);

        private async Task ValidateAsync(StudentSubjectRequest request, Guid? exceptId)
        {
            if (request.StudentId is not { } studentId || !await _students.IsExistAsync(s => s.Id == studentId))
                throw RuleViolation($"Student {request.StudentId} does not exist.");
            await _structure.EnsureSubjectExistsAsync(request.SubjectId);

            if (await Repository.IsExistAsync(m => m.StudentId == studentId && m.SubjectId == request.SubjectId
                                                   && m.AcademicYear == request.AcademicYear && m.Id != exceptId))
                throw Conflict("The student already takes this subject in that academic year.");
        }
    }
}
