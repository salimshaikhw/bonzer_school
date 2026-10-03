using System;
using System.Collections.Generic;

namespace Infrastructure;

public partial class StudentSubject
{
    public Guid Id { get; set; }

    public Guid? StudentId { get; set; }

    public Guid? SubjectId { get; set; }

    public string? AcademicYear { get; set; }
}
