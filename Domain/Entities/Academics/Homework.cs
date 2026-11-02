using Domain.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Academics;

[Table("homework")]
public partial class Homework : AuditableEntity
{

    [References(typeof(Class))]
    public Guid? ClassId { get; set; }

    [References(typeof(Subject))]
    public Guid? SubjectId { get; set; }

    public string? Description { get; set; }

    public DateOnly? DueDate { get; set; }
}
