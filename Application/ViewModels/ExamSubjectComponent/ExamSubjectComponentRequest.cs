using System;

namespace Application.ViewModels.ExamSubjectComponent;

public class ExamSubjectComponentRequest
{
    public Guid? ExamSubjectId { get; set; }
    public Guid? ComponentTypeId { get; set; }
    public decimal? MaxMarks { get; set; }
    public decimal? PassMarks { get; set; }
    public decimal? WeightagePercent { get; set; }
    public DateTime? CreatedAt { get; set; }
}
