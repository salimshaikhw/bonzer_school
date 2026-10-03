using System;

namespace Application.ViewModels.Role
{
    public class RoleRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
    }
    public class RoleResponse
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
    }
}
