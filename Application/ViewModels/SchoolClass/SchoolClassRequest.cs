using System;

namespace Application.ViewModels.SchoolClass
{
    public class SchoolClassRequest
    {
        public Guid? BranchId { get; set; }
        public string? Name { get; set; }
    }
}
