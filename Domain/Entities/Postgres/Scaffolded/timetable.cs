using System;
using System.Collections.Generic;

namespace Domain.Entities.Postgres.Scaffolded;

public partial class Timetable
{
    public Guid Id { get; set; }

    public Guid? BranchId { get; set; }

    public Guid? ClassId { get; set; }

    public Guid? SubjectId { get; set; }

    public Guid? TeacherId { get; set; }
    //public Guid? SectionId { get; set; } 

    public int? DayOfWeek { get; set; }

    public int? PeriodNo { get; set; }

    public Guid? AcademicYearId { get; set; }
}
