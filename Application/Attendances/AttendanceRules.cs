using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities.Students;

namespace Application.Attendances
{
    /// <summary>Canonical attendance statuses; reports and percentages count these exact values.</summary>
    public static class AttendanceStatuses
    {
        public const string Present = "Present";
        public const string Absent = "Absent";
        public const string Late = "Late";
        public const string HalfDay = "HalfDay";
        public const string Leave = "Leave";

        public static readonly IReadOnlyList<string> All = new[] { Present, Absent, Late, HalfDay, Leave };

        /// <summary>
        /// Maps common spellings ("P", "present", "1", "half-day", "lv") to the canonical value; null when the
        /// input is blank or not a known status.
        /// </summary>
        public static string? Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            return value.Trim().ToLowerInvariant() switch
            {
                "p" or "present" or "pr" or "1" or "yes" or "y" or "in" => Present,
                "a" or "absent" or "ab" or "0" or "no" or "n" or "out" => Absent,
                "l" or "late" => Late,
                "h" or "half" or "halfday" or "half-day" or "half day" => HalfDay,
                "lv" or "leave" => Leave,
                _ => null
            };
        }
    }

    /// <summary>
    /// The single attendance percentage rule, used by every report so they all agree:
    ///   working days = present + late + half day + absent + leave (holidays and unknown statuses do not count);
    ///   attended     = present + late (a late student was there) + half a day for each half day;
    ///   percentage   = attended / working days x 100, rounded to 2 decimals.
    /// Callers pass only the records of working days (weekly offs and calendar holidays already removed).
    /// </summary>
    public sealed record AttendanceTally(int Present, int Late, int HalfDay, int Absent, int Leave)
    {
        public int WorkingDays => Present + Late + HalfDay + Absent + Leave;

        public decimal Attended => Present + Late + HalfDay * 0.5m;

        public decimal Percentage => WorkingDays == 0 ? 0 : Math.Round(Attended / WorkingDays * 100, 2, MidpointRounding.AwayFromZero);

        public static AttendanceTally Of(IEnumerable<string?> statuses)
        {
            int present = 0, late = 0, halfDay = 0, absent = 0, leave = 0;
            foreach (var status in statuses)
            {
                switch (AttendanceStatuses.Normalize(status))
                {
                    case AttendanceStatuses.Present: present++; break;
                    case AttendanceStatuses.Late: late++; break;
                    case AttendanceStatuses.HalfDay: halfDay++; break;
                    case AttendanceStatuses.Absent: absent++; break;
                    case AttendanceStatuses.Leave: leave++; break;
                }
            }
            return new AttendanceTally(present, late, halfDay, absent, leave);
        }
    }

    /// <summary>
    /// Whether attendance may be written for a student on a date. One place for the rules used by every
    /// marking path (single mark, class register, bulk, machine import, edits and deletes).
    /// </summary>
    public interface IAttendancePolicy
    {
        /// <summary>
        /// Returns the student when attendance can be marked: the date is not in the future and not before
        /// admission; the student is on the roll that day (active or suspended, or left later than that date);
        /// the class / division, when given, are the student's; and the day is not locked.
        /// Throws 400 / 409 with the reason otherwise.
        /// </summary>
        Task<Student> EnsureCanMarkAsync(Guid studentId, DateOnly date, Guid? classId, Guid? divisionId);

        /// <summary>Throws 409 when the class / division day, or the academic year containing it, is locked.</summary>
        Task EnsureNotLockedAsync(Guid? classId, Guid? divisionId, DateOnly date);

        Task<bool> IsLockedAsync(Guid? classId, Guid? divisionId, DateOnly date);
    }
}
