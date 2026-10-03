using System.Collections.Generic;
using Application.ViewModels.Attendance;
using Application.ViewModels.Fees;
using Application.ViewModels.Mark;

namespace Application.ViewModels.Student;

/// <summary>Single payload for the admission student profile (360°) screen.</summary>
public class StudentAdmissionPortfolioDto
{
    public StudentResponse? Student { get; set; }
    public AttendanceSummaryDto? AttendanceAcademicYear { get; set; }
    public StudentFeeDashboardDto? Fees { get; set; }
    public List<MarkResponse> Marks { get; set; } = new();
}
