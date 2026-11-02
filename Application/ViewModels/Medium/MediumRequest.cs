using System;

namespace Application.ViewModels.Medium
{
    public class MediumRequest
    {
        public string? Name { get; set; }
    }
    public class MediumResponse : AuditableResponse
    {
        public string? Name { get; set; }
    }
}
