using System;

namespace Application.ViewModels.Notification
{
    public class NotificationRequest
    {
        public Guid? StudentId { get; set; }
        public Guid? BranchId { get; set; }
        public Guid? DivisionId { get; set; }
        public string? Type { get; set; }
        public string? Title { get; set; }
        public string? Message { get; set; }
        public DateTime? SentAt { get; set; }
    }
    public class NotificationResponse
    {
        public Guid Id { get; set; }
        public Guid? StudentId { get; set; }
        public Guid? BranchId { get; set; }
        public Guid? DivisionId { get; set; }
        public string? Type { get; set; }
        public string? Title { get; set; }
        public string? Message { get; set; }
        public DateTime? SentAt { get; set; }
    }
}
