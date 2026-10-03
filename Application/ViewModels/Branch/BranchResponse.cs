using System;

namespace Application.ViewModels.Branch
{
    public class BranchResponse
    {
        public Guid Id { get; set; }
        public Guid? GroupId { get; set; }
        public Guid? InstitutionTypeId { get; set; }
        public Guid? MediumId { get; set; }
        public string? Schooluid { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
