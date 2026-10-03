using System;

namespace Application.ViewModels.ExamType;

public class ExamTypeResponse
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}
