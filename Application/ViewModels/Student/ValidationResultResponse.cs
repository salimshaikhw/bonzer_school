namespace Application.ViewModels.Student
{
    /// <summary>Result of a validation check; used by Validate* endpoints.</summary>
    public class ValidationResultResponse
    {
        public bool IsValid { get; set; }
        public string? Message { get; set; }
    }
}
