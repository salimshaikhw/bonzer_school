namespace Application.ViewModels.Student
{
    /// <summary>Gender counts for a year in GenderRatioTrend.</summary>
    public class GenderRatioTrendPointDto
    {
        public int Year { get; set; }
        public int MaleCount { get; set; }
        public int FemaleCount { get; set; }
        public int OtherCount { get; set; }
    }
}
