using System;

namespace Application.ViewModels.ParentUser
{
    public class ParentUserRequest
    {
        public Guid? StudentId { get; set; }
        public string? Username { get; set; }
        public string? PasswordHash { get; set; }
        public string? Phone { get; set; }
    }
    public class ParentUserResponse
    {
        public Guid Id { get; set; }
        public Guid? StudentId { get; set; }
        public string? Username { get; set; }
        public string? PasswordHash { get; set; }
        public string? Phone { get; set; }
    }
}
