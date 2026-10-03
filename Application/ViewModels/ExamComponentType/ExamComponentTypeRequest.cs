using System;

namespace Application.ViewModels.ExamComponentType;

public class ExamComponentTypeRequest
{
    public string Name { get; set; }
    public string? Code { get; set; }
    public DateTime? CreatedAt { get; set; }
}
