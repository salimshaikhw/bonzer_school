using System;
using System.Collections.Generic;

namespace Infrastructure;

public partial class Exam
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public Guid? ExamTypeId { get; set; }

    public Guid? AcademicYearId { get; set; }

    public Guid? BranchId { get; set; }

    public Guid? ClassId { get; set; }

    public Guid? DivisionId { get; set; }

    public DateOnly? StartDate { get; set; }

    //public Guid? SectionId { get; set; }
    public DateOnly? EndDate { get; set; }

    public string? Status { get; set; }

    public bool? IsLocked { get; set; }

    public DateTime? CreatedAt { get; set; }
}
