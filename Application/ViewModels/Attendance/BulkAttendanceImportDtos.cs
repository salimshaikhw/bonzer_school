using System;
using System.Collections.Generic;

namespace Application.ViewModels.Attendance;

/// <summary>Machine / Excel bulk import: resolve students in a branch (optional class/division scope).</summary>
public class BulkAttendanceImportRequest
{
    public Guid BranchId { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? DivisionId { get; set; }
    public Guid? ClassTeacherId { get; set; }

    /// <summary>When a row has no date, use this (e.g. thumb file is for one day only).</summary>
    public DateOnly? DefaultDate { get; set; }

    /// <summary>If true, blank status becomes Present (typical for punch logs).</summary>
    public bool DefaultPresentWhenStatusEmpty { get; set; } = true;

    public List<BulkAttendanceImportRowInput> Rows { get; set; } = new();
}

public class BulkAttendanceImportRowInput
{
    /// <summary>Original row number in file (1-based) for error reporting.</summary>
    public int RowIndex { get; set; }

    public Guid? StudentId { get; set; }
    public string? RollNumber { get; set; }
    public string? AdmissionNo { get; set; }
    public string? EnrollmentNo { get; set; }

    /// <summary>Attendance date (alias: <see cref="AttendanceDate"/>).</summary>
    public DateOnly? Date { get; set; }

    /// <summary>Same as Date; matches Excel column AttendanceDate.</summary>
    public DateOnly? AttendanceDate { get; set; }

    /// <summary>Class for this row (with RollNumber, use with DivisionName or request DivisionId).</summary>
    public Guid? ClassId { get; set; }

    /// <summary>Division name within <see cref="ClassId"/> (resolved to DivisionId).</summary>
    public string? DivisionName { get; set; }

    public string? Status { get; set; }
    public string? LeaveType { get; set; }

    /// <summary>Optional explicit holiday flag from file (true forces holiday together with weekly off / calendar rules in <see cref="MarkAttendanceDto"/>).</summary>
    public bool? IsHoliday { get; set; }

    public string? Remark { get; set; }
}

public class BulkAttendanceImportPreviewDto
{
    public List<BulkAttendanceImportRowResultDto> Rows { get; set; } = new();
    public int TotalRows { get; set; }
    public int ResolvedCount { get; set; }
    public int ErrorCount { get; set; }
    public int WouldImportCount { get; set; }
    public int HolidayAwareRowCount { get; set; }
}

public class BulkAttendanceImportRowResultDto
{
    public int RowIndex { get; set; }
    public bool Resolved { get; set; }
    public Guid? StudentId { get; set; }
    public string? StudentDisplayName { get; set; }
    public string? Error { get; set; }
    public DateOnly? Date { get; set; }
    public string? RawStatus { get; set; }
    public string? NormalizedStatus { get; set; }
    public Guid? ResolvedClassId { get; set; }
    public Guid? ResolvedDivisionId { get; set; }
    public string? LeaveType { get; set; }
    public bool? RowIsHoliday { get; set; }
    public bool IsWeeklyOff { get; set; }
    public bool IsCalendarHoliday { get; set; }
    public bool EffectiveHoliday { get; set; }
    public bool WouldImport { get; set; }
}

public class BulkAttendanceImportApplyResultDto : BulkAttendanceImportPreviewDto
{
    public int SavedCount { get; set; }
}

/// <summary>Build a pre-filled attendance import workbook for one division and a date range.</summary>
public class AttendanceDivisionTemplateRequest
{
    public Guid BranchId { get; set; }
    public Guid ClassId { get; set; }
    public Guid DivisionId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}
