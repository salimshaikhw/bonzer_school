using System;

namespace Application.ViewModels.ExamComponentType;

public class ExamComponentTypeResponse : AuditableResponse
{
    public string Name { get; set; }
    public string? Code { get; set; }
}
