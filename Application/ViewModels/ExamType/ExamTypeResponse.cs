using System;

namespace Application.ViewModels.ExamType;

public class ExamTypeResponse : AuditableResponse
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}
