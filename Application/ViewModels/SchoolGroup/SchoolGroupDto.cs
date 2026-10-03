using System;

namespace Application.ViewModels.SchoolGroup
{
    public class SchoolGroupDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class CreateSchoolGroupInput
    {

        public string Name { get; set; }
        public string Code { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }

    public class UpdateSchoolGroupInput
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }
}