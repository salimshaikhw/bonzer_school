using System;

namespace Application.ViewModels.Student
{
    /// <summary>Profile summary for a student (demographics, contact, status).</summary>
    public class StudentProfileSummaryDto
    {
        public Guid StudentId { get; set; }
        public string? FullName { get; set; }
        public string? AdmissionNo { get; set; }
        public string? RollNumber { get; set; }
        public string? Gender { get; set; }
        public DateOnly? Dob { get; set; }
        public int? AgeYears { get; set; }
        public string? BloodGroup { get; set; }
        public string? Category { get; set; }
        public string? Religion { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? StudentStatus { get; set; }
        public bool? IsActive { get; set; }
        public string? PhotoUrl { get; set; }
    }

    /// <summary>Academic context: class, division, branch, academic year.</summary>
    public class StudentAcademicSummaryDto
    {
        public Guid StudentId { get; set; }
        public string? ClassName { get; set; }
        public Guid? ClassId { get; set; }
        public string? DivisionName { get; set; }
        public Guid? DivisionId { get; set; }
        public string? BranchName { get; set; }
        public Guid? BranchId { get; set; }
        public string? AcademicYearName { get; set; }
        public Guid? AcademicYearId { get; set; }
        public DateOnly? AdmissionDate { get; set; }
        public string? RollNumber { get; set; }
        public string? EnrollmentNo { get; set; }
        public string? LastExamName { get; set; }
        public string? LastExamResult { get; set; }
    }

    /// <summary>Fee summary: totals and outstanding for the student.</summary>
    public class StudentFeeSummaryDto
    {
        public Guid StudentId { get; set; }
        public int TotalInvoices { get; set; }
        public decimal TotalBilled { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalOutstanding { get; set; }
        public int InvoicesOverdue { get; set; }
    }

    /// <summary>Attendance summary for a period (e.g. current month).</summary>
    public class StudentAttendanceSummaryDto
    {
        public Guid StudentId { get; set; }
        public DateOnly? PeriodStart { get; set; }
        public DateOnly? PeriodEnd { get; set; }
        public int TotalDays { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LeaveDays { get; set; }
        public decimal AttendancePercent { get; set; }
    }

    /// <summary>Age in years and months (from DOB to reference date).</summary>
    public class StudentAgeDto
    {
        public Guid StudentId { get; set; }
        public DateOnly? Dob { get; set; }
        public int? Years { get; set; }
        public int? Months { get; set; }
        public string? Display { get; set; }
    }

    /// <summary>Duration from admission date to reference date.</summary>
    public class StudentAcademicDurationDto
    {
        public Guid StudentId { get; set; }
        public DateOnly? AdmissionDate { get; set; }
        public int? Years { get; set; }
        public int? Months { get; set; }
        public string? Display { get; set; }
    }
}
