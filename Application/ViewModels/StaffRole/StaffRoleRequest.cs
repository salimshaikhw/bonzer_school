using System;

namespace Application.ViewModels.StaffRole
{
    public class StaffRoleRequest
    {
        public Guid? StaffId { get; set; }
        public Guid? RoleId { get; set; }
    }
    public class StaffRoleResponse : AuditableResponse
    {
        public Guid? StaffId { get; set; }
        public Guid? RoleId { get; set; }
    }
}
