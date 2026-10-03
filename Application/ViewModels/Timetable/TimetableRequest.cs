using System;

namespace Application.ViewModels.Timetable
{
    public class TimetableRequest
    {
        public Guid? BranchId { get; set; }
        public Guid? ClassId { get; set; }
        public Guid? SubjectId { get; set; }
        public Guid? TeacherId { get; set; }
        public int? DayOfWeek { get; set; }
        public int? PeriodNo { get; set; }
        public Guid? AcademicYearId { get; set; }
    }
    public class TimetableResponse
    {
        public Guid Id { get; set; }
        public Guid? BranchId { get; set; }
        public Guid? ClassId { get; set; }
        public Guid? SubjectId { get; set; }
        public Guid? TeacherId { get; set; }
        public int? DayOfWeek { get; set; }
        public int? PeriodNo { get; set; }
        public Guid? AcademicYearId { get; set; }
    }
}
