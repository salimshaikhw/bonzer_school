using System;
using System.Collections.Generic;
using System.Linq;
using Application.Common;
using Application.Exceptions;
using Domain.Entities.Students;

namespace Application.Students
{
    /// <summary>Values of students.student_status.</summary>
    public static class StudentStatuses
    {
        public const string Active = "ACTIVE";
        public const string Suspended = "SUSPENDED";
        public const string Dropped = "DROPPED";
        /// <summary>Left the school group with a transfer certificate.</summary>
        public const string Transferred = "TRANSFERRED";
        public const string PassedOut = "PASSED_OUT";

        public static readonly IReadOnlyList<string> All = new[] { Active, Suspended, Dropped, Transferred, PassedOut };

        /// <summary>Upper-case, trimmed status; legacy rows without a status count as active.</summary>
        public static string Of(Student student) =>
            string.IsNullOrWhiteSpace(student.StudentStatus) ? Active : student.StudentStatus.Trim().ToUpperInvariant();

        public static bool IsKnown(string? status) => status != null && All.Contains(status.Trim().ToUpperInvariant());
    }

    public enum StudentAction
    {
        Activate,
        Suspend,
        Drop,
        /// <summary>Move to another branch of the same school group; the student stays active there.</summary>
        TransferBranch,
        Promote,
        PassOut,
        /// <summary>Issue a transfer / leaving certificate: the student leaves the school group.</summary>
        IssueTc,
        ReEnroll,
    }

    /// <summary>
    /// State machine of a student's enrolment (State pattern as a transition table). It is the single place
    /// that decides which action is allowed from which status and what it changes, so the lifecycle
    /// endpoints and the "is eligible?" checks can never disagree.
    ///
    ///   ACTIVE ──suspend──► SUSPENDED ──activate──► ACTIVE
    ///   ACTIVE / SUSPENDED ──drop──► DROPPED
    ///   ACTIVE ──pass out──► PASSED_OUT
    ///   ACTIVE / SUSPENDED / PASSED_OUT / DROPPED ──issue TC──► TRANSFERRED
    ///   DROPPED / TRANSFERRED ──re-enrol──► ACTIVE
    ///   ACTIVE ──promote / transfer branch──► ACTIVE (class or branch changes)
    /// Leaving (drop, pass out, TC) deactivates the student and records the leaving date.
    /// </summary>
    public static class StudentLifecycle
    {
        private static readonly IReadOnlyDictionary<StudentAction, (string[] From, string To)> Transitions =
            new Dictionary<StudentAction, (string[], string)>
            {
                [StudentAction.Activate] = (new[] { StudentStatuses.Suspended }, StudentStatuses.Active),
                [StudentAction.Suspend] = (new[] { StudentStatuses.Active }, StudentStatuses.Suspended),
                [StudentAction.Drop] = (new[] { StudentStatuses.Active, StudentStatuses.Suspended }, StudentStatuses.Dropped),
                [StudentAction.TransferBranch] = (new[] { StudentStatuses.Active }, StudentStatuses.Active),
                [StudentAction.Promote] = (new[] { StudentStatuses.Active }, StudentStatuses.Active),
                [StudentAction.PassOut] = (new[] { StudentStatuses.Active }, StudentStatuses.PassedOut),
                [StudentAction.IssueTc] = (new[] { StudentStatuses.Active, StudentStatuses.Suspended, StudentStatuses.PassedOut, StudentStatuses.Dropped }, StudentStatuses.Transferred),
                [StudentAction.ReEnroll] = (new[] { StudentStatuses.Dropped, StudentStatuses.Transferred }, StudentStatuses.Active),
            };

        /// <summary>Null when <paramref name="action"/> is allowed for the student; otherwise the reason it is not.</summary>
        public static string? WhyNot(Student student, StudentAction action)
        {
            var current = StudentStatuses.Of(student);
            var (from, _) = Transitions[action];
            if (!from.Contains(current))
                return $"Cannot {Describe(action)} a student whose status is {current} (allowed from: {string.Join(", ", from)}).";
            if (action == StudentAction.IssueTc && student.IsTcIssued == true)
                return "A transfer certificate has already been issued to this student.";
            if (action == StudentAction.Promote && student.ClassId == null)
                return "The student has no current class to be promoted from.";
            return null;
        }

        public static bool Allows(Student student, StudentAction action) => WhyNot(student, action) == null;

        /// <summary>Applies the action's status change and side effects, or throws a 409 explaining why it is not allowed.</summary>
        public static void Apply(Student student, StudentAction action, DateOnly today)
        {
            var reason = WhyNot(student, action);
            if (reason != null)
                throw new BaseServiceException(reason, ExceptionCodes.Operation);

            student.StudentStatus = Transitions[action].To;
            switch (action)
            {
                case StudentAction.Activate:
                    student.IsActive = true;
                    break;
                case StudentAction.Drop:
                case StudentAction.PassOut:
                    Leave(student, today);
                    break;
                case StudentAction.IssueTc:
                    Leave(student, today);
                    student.IsTcIssued = true;
                    break;
                case StudentAction.ReEnroll:
                    // A new enrolment period: the old leaving date and TC belong to the previous one.
                    student.IsActive = true;
                    student.LeavingDate = null;
                    student.IsTcIssued = false;
                    break;
            }
        }

        /// <summary>The action that moves a student to <paramref name="targetStatus"/>, for the generic "set status" endpoint.</summary>
        public static StudentAction ActionFor(Student student, string targetStatus)
        {
            var target = targetStatus?.Trim().ToUpperInvariant();
            return target switch
            {
                StudentStatuses.Active when StudentStatuses.Of(student) == StudentStatuses.Suspended => StudentAction.Activate,
                StudentStatuses.Active => StudentAction.ReEnroll,
                StudentStatuses.Suspended => StudentAction.Suspend,
                StudentStatuses.Dropped => StudentAction.Drop,
                StudentStatuses.PassedOut => StudentAction.PassOut,
                StudentStatuses.Transferred => StudentAction.IssueTc,
                _ => throw new BaseServiceException(
                    $"Unknown student status '{targetStatus}'. Use one of: {string.Join(", ", StudentStatuses.All)}.", ExceptionCodes.Validation)
            };
        }

        private static void Leave(Student student, DateOnly today)
        {
            student.IsActive = false;
            student.LeavingDate ??= today;
        }

        private static string Describe(StudentAction action) => action switch
        {
            StudentAction.TransferBranch => "transfer",
            StudentAction.PassOut => "pass out",
            StudentAction.IssueTc => "issue a TC to",
            StudentAction.ReEnroll => "re-enrol",
            _ => action.ToString().ToLowerInvariant()
        };
    }
}
