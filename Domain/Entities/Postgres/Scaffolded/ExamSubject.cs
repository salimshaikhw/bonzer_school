using System;
using System.Collections.Generic;

namespace Infrastructure;

public partial class ExamSubject
{
    public Guid Id { get; set; }

    public Guid? ExamId { get; set; }

    public Guid? SubjectId { get; set; }

    public decimal? TotalMaxMarks { get; set; }

    public decimal? TotalPassMarks { get; set; }

    public DateOnly? ExamDate { get; set; }

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }

    public int? DurationMinutes { get; set; }

    public DateTime? CreatedAt { get; set; }
}
