using Application.Common;
using Application.Exceptions;

namespace Infrastructure.Services.Exams
{
    /// <summary>
    /// Navigates exam -> subject -> component for the Exams services and enforces the lock: once an exam is
    /// locked (results finalised), its subjects, components and marks can no longer change.
    /// </summary>
    public class ExamStructure
    {
        private readonly IExamRepository _exams;
        private readonly IExamSubjectRepository _subjects;
        private readonly IExamSubjectComponentRepository _components;

        public ExamStructure(IExamRepository exams, IExamSubjectRepository subjects, IExamSubjectComponentRepository components)
        {
            _exams = exams;
            _subjects = subjects;
            _components = components;
        }

        public async Task<Exam> GetExamAsync(Guid? examId)
        {
            if (examId is not { } id)
                throw Invalid("An exam is required.");
            return await _exams.FindAsync(e => e.Id == id) ?? throw Invalid($"Exam {id} does not exist.");
        }

        public async Task<(ExamSubject Subject, Exam Exam)> GetExamSubjectAsync(Guid? examSubjectId)
        {
            if (examSubjectId is not { } id)
                throw Invalid("An exam subject is required.");
            var subject = await _subjects.FindAsync(s => s.Id == id) ?? throw Invalid($"Exam subject {id} does not exist.");
            return (subject, await GetExamAsync(subject.ExamId));
        }

        public async Task<(ExamSubjectComponent Component, ExamSubject Subject, Exam Exam)> GetComponentAsync(Guid? componentId)
        {
            if (componentId is not { } id)
                throw Invalid("An exam component is required.");
            var component = await _components.FindAsync(c => c.Id == id) ?? throw Invalid($"Exam component {id} does not exist.");
            var (subject, exam) = await GetExamSubjectAsync(component.ExamSubjectId);
            return (component, subject, exam);
        }

        public static void EnsureUnlocked(Exam exam)
        {
            if (exam.IsLocked == true)
                throw new BaseServiceException($"Exam '{exam.Name}' is locked. Unlock it before changing its subjects, components or marks.", ExceptionCodes.Operation);
        }

        /// <summary>
        /// Whether a student sits an exam (shared by marks and hall tickets): on the roll (or left after the exam
        /// started), and for an exam of the student's current academic year, in its class and division.
        /// Throws 409 (not on roll) or 400 (wrong class / division).
        /// </summary>
        public static void EnsureStudentSitsExam(Domain.Entities.Students.Student student, Exam exam)
        {
            var status = Application.Students.StudentStatuses.Of(student);
            if (status is not (Application.Students.StudentStatuses.Active or Application.Students.StudentStatuses.Suspended)
                && !(student.LeavingDate is { } left && exam.StartDate is { } start && start <= left))
                throw new BaseServiceException($"The student is {status} and does not sit this exam.", ExceptionCodes.Operation);
            if (student.AcademicYearId != null && student.AcademicYearId == exam.AcademicYearId)
            {
                if (exam.ClassId != null && student.ClassId != exam.ClassId)
                    throw Invalid("The student is not in the class of this exam.");
                if (exam.DivisionId != null && student.DivisionId != exam.DivisionId)
                    throw Invalid("The student is not in the division of this exam.");
            }
        }

        private static BaseServiceException Invalid(string message) => new(message, ExceptionCodes.Validation);
    }
}
