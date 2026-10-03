using System;

namespace Application.ViewModels.ExamComponentType;

public class ExamComponentTypeResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Code { get; set; }
    public DateTime? CreatedAt { get; set; }
}
