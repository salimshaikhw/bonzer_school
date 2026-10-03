using System;
using System.Collections.Generic;

namespace Infrastructure;

public partial class ClassSubjectMap
{
    public Guid Id { get; set; }

    public Guid? ClassId { get; set; }

    public Guid? DivisionId { get; set; }

    public Guid? SubjectId { get; set; }
    //public Guid? SectionId { get; set; }

    public string? AcademicYear { get; set; }

}
