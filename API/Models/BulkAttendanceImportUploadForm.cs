using System;
using Microsoft.AspNetCore.Http;

namespace API.Models;

/// <summary>Multipart upload for bulk attendance import (.xlsx, .xls, .csv).</summary>
public class BulkAttendanceImportUploadForm
{
    public Guid BranchId { get; set; }

    public Guid? ClassId { get; set; }

    public Guid? DivisionId { get; set; }

    public Guid? ClassTeacherId { get; set; }

    /// <summary>ISO date yyyy-MM-dd when rows omit AttendanceDate/Date.</summary>
    public string? DefaultDate { get; set; }

    public bool DefaultPresentWhenStatusEmpty { get; set; } = true;

    public IFormFile? File { get; set; }
}
