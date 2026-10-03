using System;

namespace Infrastructure;

public partial class HallTicket
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid ExamId { get; set; }
    public Guid? AcademicYearId { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? DivisionId { get; set; }
    public Guid? BranchId { get; set; }
    public string? TicketNumber { get; set; }
    public string? SeatNumber { get; set; }
    public string? ExamCentre { get; set; }
    public DateTime? GeneratedAt { get; set; }
}
