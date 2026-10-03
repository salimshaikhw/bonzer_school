namespace Application.ViewModels.Student
{
    /// <summary>Caste distribution for a year in CasteTrendAnalysis.</summary>
    public class CasteTrendPointDto
    {
        public int Year { get; set; }
        public string Caste { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
