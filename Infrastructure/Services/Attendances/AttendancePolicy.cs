using Application.Attendances;
using Application.Common;
using Application.Exceptions;
using Application.Students;

namespace Infrastructure.Services.Attendances
{
    /// <inheritdoc cref="IAttendancePolicy"/>
    public class AttendancePolicy : IAttendancePolicy
    {
        private readonly IStudentRepository _students;
        private readonly IAttendanceDateLockRepository _dateLocks;
        private readonly IAttendanceAcademicYearLockRepository _yearLocks;
        private readonly IAcademicYearRepository _years;
        private readonly TimeProvider _clock;

        public AttendancePolicy(IStudentRepository students, IAttendanceDateLockRepository dateLocks,
            IAttendanceAcademicYearLockRepository yearLocks, IAcademicYearRepository years, TimeProvider clock)
        {
            _students = students;
            _dateLocks = dateLocks;
            _yearLocks = yearLocks;
            _years = years;
            _clock = clock;
        }

        public async Task<Student> EnsureCanMarkAsync(Guid studentId, DateOnly date, Guid? classId, Guid? divisionId)
        {
            // One day of slack: the server runs in UTC, schools may be ahead of it.
            if (date > DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime).AddDays(1))
                throw Invalid("Attendance cannot be marked for a future date.");

            var student = await _students.GetByIdAsync(studentId);
            if (student.AdmissionDate is { } admitted && date < admitted)
                throw Invalid($"The student was admitted on {admitted:dd-MM-yyyy}; attendance before that date cannot be marked.");

            var status = StudentStatuses.Of(student);
            var onRoll = status is StudentStatuses.Active or StudentStatuses.Suspended
                         || (student.LeavingDate is { } left && date <= left);
            if (!onRoll)
                throw new BaseServiceException($"The student is {status} and was not on the roll on {date:dd-MM-yyyy}.", ExceptionCodes.Operation);

            if (classId != null && student.ClassId != null && classId != student.ClassId)
                throw Invalid("The student is not in this class.");
            if (divisionId != null && student.DivisionId != null && divisionId != student.DivisionId)
                throw Invalid("The student is not in this division.");

            await EnsureNotLockedAsync(classId ?? student.ClassId, divisionId ?? student.DivisionId, date);
            return student;
        }

        public async Task EnsureNotLockedAsync(Guid? classId, Guid? divisionId, DateOnly date)
        {
            if (await IsLockedAsync(classId, divisionId, date))
                throw new BaseServiceException($"Attendance for {date:dd-MM-yyyy} is locked. Ask an administrator to unlock it first.", ExceptionCodes.Operation);
        }

        public async Task<bool> IsLockedAsync(Guid? classId, Guid? divisionId, DateOnly date)
        {
            // A day lock covers its division; a lock without a division covers the whole class. Asked about the
            // whole class (no division), any locked division of it counts.
            if (classId != null
                && await _dateLocks.IsExistAsync(l => l.IsLocked && l.ClassId == classId && l.AttendanceDate == date
                                                     && (divisionId == null || l.DivisionId == null || l.DivisionId == divisionId)))
                return true;

            var yearIds = (await _years.FindAllAsync(y => y.StartDate <= date && y.EndDate >= date)).Select(y => y.Id).ToList();
            return yearIds.Count > 0
                   && await _yearLocks.IsExistAsync(l => l.IsLocked && l.AcademicYearId != null && yearIds.Contains(l.AcademicYearId.Value));
        }

        private static BaseServiceException Invalid(string message) => new(message, ExceptionCodes.Validation);
    }
}
