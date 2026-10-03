using System;
using System.Collections.Generic;

namespace Infrastructure;

public partial class StudentResult
{
    public Guid Id { get; set; }

    public Guid? ExamId { get; set; }

    public Guid? StudentId { get; set; }

    public decimal? TotalMarks { get; set; }

    public decimal? Percentage { get; set; }

    public string? Grade { get; set; }

    public string? Result { get; set; }

    public int? Rank { get; set; }

    public DateTime? GeneratedAt { get; set; }
}
