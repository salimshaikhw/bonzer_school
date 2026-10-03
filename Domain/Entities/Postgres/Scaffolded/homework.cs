using System;
using System.Collections.Generic;

namespace Infrastructure;

public partial class Homework
{
    public Guid Id { get; set; }

    public Guid? ClassId { get; set; }

    public Guid? SubjectId { get; set; }

    public string? Description { get; set; }

    public DateOnly? DueDate { get; set; }
}
