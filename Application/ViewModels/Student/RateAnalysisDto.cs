using System;

namespace Application.ViewModels.Student
{
    /// <summary>Rate analysis (dropout, transfer, retention, promotion, conversion).</summary>
    public class RateAnalysisDto
    {
        public decimal Rate { get; set; }
        public int TotalCount { get; set; }
        public int Count { get; set; }
        public string? Label { get; set; }
        public string? Message { get; set; }
    }
}
