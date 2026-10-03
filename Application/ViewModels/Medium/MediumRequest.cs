using System;

namespace Application.ViewModels.Medium
{
    public class MediumRequest
    {
        public string? Name { get; set; }
    }
    public class MediumResponse
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
    }
}
