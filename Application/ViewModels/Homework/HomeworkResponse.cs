namespace Application.ViewModels.Homework
{
    public class HomeworkResponse : AuditableResponse
    {
        public Guid? ClassId { get; set; }
        public Guid? SubjectId { get; set; }
        public string? Description { get; set; }
        public DateOnly? DueDate { get; set; }
    }
}
