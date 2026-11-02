using System;

namespace Application.ViewModels.ExamSubjectComponent;

public class ExamSubjectComponentResponse : AuditableResponse
{
    public Guid? ExamSubjectId { get; set; }
    public Guid? ComponentTypeId { get; set; }
    public decimal? MaxMarks { get; set; }
    public decimal? PassMarks { get; set; }
    public decimal? WeightagePercent { get; set; }
}
