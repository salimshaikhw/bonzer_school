using System;
using System.Collections.Generic;

namespace Application.ViewModels.Certificates;

public class HallTicketResponse
{
    public Guid Id { get; set; }
    public string? TicketNumber { get; set; }
    public string? SeatNumber { get; set; }
    public string? ExamCentre { get; set; }
    public DateTime? GeneratedAt { get; set; }

    public Guid StudentId { get; set; }
    public string? StudentName { get; set; }
    public string? AdmissionNo { get; set; }
    public Guid ExamId { get; set; }
    public string? ExamName { get; set; }
    public DateOnly? ExamStartDate { get; set; }
    public DateOnly? ExamEndDate { get; set; }
    public string? AcademicYearName { get; set; }
    public string? ClassName { get; set; }
    public string? DivisionName { get; set; }

    public List<HallTicketPaperDto> Papers { get; set; } = new();
}

public class HallTicketPaperDto
{
    public Guid SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public DateOnly? ExamDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
}
