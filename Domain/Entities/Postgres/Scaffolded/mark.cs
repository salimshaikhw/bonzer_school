using System;
using System.Collections.Generic;

namespace Infrastructure;

public partial class Mark
{
    public Guid Id { get; set; }

    public Guid? ExamSubjectComponentId { get; set; }

    public Guid? StudentId { get; set; }

    public decimal? MarksObtained { get; set; }

    public bool? IsAbsent { get; set; }

    public bool? IsMedical { get; set; }

    public bool? IsExempt { get; set; }

    public string? Remark { get; set; }

    public Guid? EnteredBy { get; set; }

    public DateTime? EnteredAt { get; set; }
}
