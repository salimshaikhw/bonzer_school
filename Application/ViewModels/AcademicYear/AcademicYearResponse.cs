using System;

namespace Application.ViewModels.AcademicYear
{
    public class AcademicYearResponse
    {
        public Guid Id { get; set; }
        public string? YearName { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public bool? IsActive { get; set; }
    }
}
