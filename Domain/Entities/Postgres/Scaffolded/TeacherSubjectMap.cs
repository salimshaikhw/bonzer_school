using System;
using System.Collections.Generic;

namespace Domain.Entities.Postgres.Scaffolded;

public partial class TeacherSubjectMap
{
    public Guid Id { get; set; }

    public Guid? StaffId { get; set; }

    public Guid? SubjectId { get; set; }

    public Guid? SectionId { get; set; }  
    public Guid? ClassId { get; set; }

    public Guid? DivisionId { get; set; }

    public string? AcademicYear { get; set; }

    public DateTime? CreatedAt { get; set; }
}
