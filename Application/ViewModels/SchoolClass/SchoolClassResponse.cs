using System;

namespace Application.ViewModels.SchoolClass
{
    public class SchoolClassResponse
    {
        public Guid Id { get; set; }
        public Guid? BranchId { get; set; }
        public string? Name { get; set; }
    }
}
