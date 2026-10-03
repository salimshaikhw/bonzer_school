using System;

namespace Application.ViewModels.ExamSubjectComponent;

public class ExamSubjectComponentResponse
{
    public Guid Id { get; set; }
    public Guid? ExamSubjectId { get; set; }
    public Guid? ComponentTypeId { get; set; }
    public decimal? MaxMarks { get; set; }
    public decimal? PassMarks { get; set; }
    public decimal? WeightagePercent { get; set; }
    public DateTime? CreatedAt { get; set; }
}
