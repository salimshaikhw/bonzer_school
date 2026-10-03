using System.Collections.Generic;

namespace Application.ViewModels.Student
{
    /// <summary>Result of a student bulk upload from Excel.</summary>
    public class StudentBulkUploadResultDto
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<StudentBulkUploadErrorDto> Errors { get; set; } = new List<StudentBulkUploadErrorDto>();
        /// <summary>Optional Excel file (.xlsx) containing all failed rows plus error messages, encoded as bytes.</summary>
        public byte[]? FailedExcelFile { get; set; }
    }

    public class StudentBulkUploadErrorDto
    {
        public int RowIndex { get; set; }
        public string? AdmissionNo { get; set; }
        public string Message { get; set; } = string.Empty;
    }


    public class StudentBulkUploadRequest
    {
        public Microsoft.AspNetCore.Http.IFormFile File { get; set; }
    }

}
