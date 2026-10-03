using System;

namespace Application.ViewModels.StaffRole
{
    public class StaffRoleRequest
    {
        public Guid? StaffId { get; set; }
        public Guid? RoleId { get; set; }
    }
    public class StaffRoleResponse
    {
        public Guid Id { get; set; }
        public Guid? StaffId { get; set; }
        public Guid? RoleId { get; set; }
    }
}
