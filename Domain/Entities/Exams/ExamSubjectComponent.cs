using Domain.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Exams;

[Table("exam_subject_components")]
[Index(nameof(ExamSubjectId))]
public partial class ExamSubjectComponent : AuditableEntity
{

    [References(typeof(ExamSubject))]
    public Guid? ExamSubjectId { get; set; }

    [References(typeof(ExamComponentType))]
    public Guid? ComponentTypeId { get; set; }

    [Precision(5, 2)]
    public decimal? MaxMarks { get; set; }

    [Precision(5, 2)]
    public decimal? PassMarks { get; set; }

    [Precision(5, 2)]
    public decimal? WeightagePercent { get; set; }
}
