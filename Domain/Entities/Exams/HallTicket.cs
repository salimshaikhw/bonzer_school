using Domain.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Exams;

[Table("hall_tickets")]
[Index(nameof(StudentId), nameof(ExamId), IsUnique = true)]
public partial class HallTicket : AuditableEntity
{
    [References(typeof(Student))]
    public Guid StudentId { get; set; }
    [References(typeof(Exam))]
    public Guid ExamId { get; set; }
    [References(typeof(AcademicYear))]
    public Guid? AcademicYearId { get; set; }
    [References(typeof(Class))]
    public Guid? ClassId { get; set; }
    [References(typeof(Division))]
    public Guid? DivisionId { get; set; }
    [References(typeof(Branch))]
    public Guid? BranchId { get; set; }
    [MaxLength(100)]
    public string? TicketNumber { get; set; }
    [MaxLength(50)]
    public string? SeatNumber { get; set; }
    [MaxLength(200)]
    public string? ExamCentre { get; set; }
    [DefaultValueSql("CURRENT_TIMESTAMP")]
    public DateTime? GeneratedAt { get; set; }
}
