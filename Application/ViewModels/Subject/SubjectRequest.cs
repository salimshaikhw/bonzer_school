using System;

namespace Application.ViewModels.Subject
{
    public class SubjectRequest
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
    }
    public class SubjectResponse
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Code { get; set; }
    }
}
