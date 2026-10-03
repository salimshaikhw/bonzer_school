namespace Application.ViewModels.Student
{
    /// <summary>Key-count pair for distribution reports (gender, category, city, etc.).</summary>
    public class DistributionItemDto
    {
        public string Key { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
