using System;

namespace Application.ViewModels.Certificates;

public class HallTicketRequest
{
    public Guid StudentId { get; set; }
    public Guid ExamId { get; set; }
    public string? SeatNumber { get; set; }
    public string? ExamCentre { get; set; }
}
