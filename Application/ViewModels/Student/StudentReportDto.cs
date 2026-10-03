using System;
using System.Collections.Generic;

namespace Application.ViewModels.Student
{
    /// <summary>Generic student report with title, generated time, summary sections and optional student list.</summary>
    public class StudentReportDto
    {
        public string ReportTitle { get; set; } = string.Empty;
        public DateTime GeneratedOn { get; set; }
        public int TotalCount { get; set; }
        public string? Description { get; set; }
        public List<ReportSectionDto> Sections { get; set; } = new List<ReportSectionDto>();
        public List<StudentResponse>? Students { get; set; }
    }

    /// <summary>Section of a report (e.g. category-wise breakdown).</summary>
    public class ReportSectionDto
    {
        public string Title { get; set; } = string.Empty;
        public List<DistributionItemDto> Items { get; set; } = new List<DistributionItemDto>();
    }
}
