using System;
using System.Collections.Generic;
using System.IO;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Application.Common.Helpers
{
    /// <summary>NPOI-based (open source) helper for student bulk upload: template generation and Excel parsing.</summary>
    public static class StudentBulkUploadExcelHelper
    {
        /// <summary>Column headers for the bulk upload template. Must match parsing keys (case-insensitive).</summary>
        public static readonly string[] TemplateColumns =
        {
            "AdmissionNo", "FirstName", "MiddleName", "LastName", "Gender", "Dob", "AdmissionDate",
            "BranchId", "AcademicYearId", "ClassId", "DivisionId", "RollNumber", "StudentStatus",
            "Phone", "Email", "AddressLine1", "AddressLine2", "City", "State", "Pincode",
            "FatherName", "FatherPhone", "MotherName", "MotherPhone", "GuardianName", "GuardianPhone",
            "Category", "Religion", "Caste", "ChildAadhaar", "BirthCertificateNo", "BloodGroup", "EnrollmentNo",
            "PreviousSchool", "PreviousClass", "Nationality"
        };

        /// <summary>Creates a template Excel file (.xlsx) with header row and one instruction row. Uses NPOI (open source).</summary>
        public static byte[] CreateTemplate()
        {
            using var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("Students");

            // Header row
            var headerRow = sheet.CreateRow(0);
            for (int i = 0; i < TemplateColumns.Length; i++)
            {
                var cell = headerRow.CreateCell(i);
                cell.SetCellValue(TemplateColumns[i]);
            }

            // Instruction row (row 1)
            var instructionRow = sheet.CreateRow(1);
            var instructionCell = instructionRow.CreateCell(0);
            instructionCell.SetCellValue("Fill from row 2. Dates as yyyy-MM-dd. GUIDs as text. Leave optional columns empty.");

            using var ms = new MemoryStream();
            workbook.Write(ms, false);
            return ms.ToArray();
        }

        /// <summary>Parses Excel stream into rows of string key-value pairs. Handles date cells. Data starts at row 2 (0=header, 1=instructions).</summary>
        public static List<Dictionary<string, string>> ParseRows(Stream excelStream)
        {
            var result = new List<Dictionary<string, string>>();
            using var workbook = new XSSFWorkbook(excelStream);
            var sheet = workbook.GetSheetAt(0);
            if (sheet == null || sheet.LastRowNum < 1) return result;

            var headerRow = sheet.GetRow(0);
            if (headerRow == null) return result;

            var columnCount = headerRow.LastCellNum;
            var headers = new List<string>();
            for (int c = 0; c < columnCount; c++)
            {
                var name = GetCellValueAsString(headerRow.GetCell(c));
                headers.Add(string.IsNullOrWhiteSpace(name) ? $"Column{c}" : name.Trim());
            }

            // Data from row 2 (index 2) onward
            for (int rowIdx = 2; rowIdx <= sheet.LastRowNum; rowIdx++)
            {
                var row = sheet.GetRow(rowIdx);
                if (row == null) continue;

                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int c = 0; c < headers.Count && c < row.LastCellNum; c++)
                {
                    var key = headers[c];
                    var value = GetCellValueAsString(row.GetCell(c));
                    dict[key] = value ?? string.Empty;
                }
                // Skip completely empty rows
                var hasAny = false;
                foreach (var v in dict.Values) { if (!string.IsNullOrWhiteSpace(v)) { hasAny = true; break; } }
                if (hasAny)
                    result.Add(dict);
            }

            return result;
        }

        /// <summary>
        /// Creates an Excel file (.xlsx) containing only the failed rows plus an extra \"Error\" column.
        /// Uses the same column order as the main template.
        /// </summary>
        public static byte[] CreateFailedRowsExcel(IEnumerable<(Dictionary<string, string> Row, string Error)> rowsWithErrors)
        {
            using var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("FailedRows");

            // Header row
            var headerRow = sheet.CreateRow(0);
            for (int i = 0; i < TemplateColumns.Length; i++)
            {
                var cell = headerRow.CreateCell(i);
                cell.SetCellValue(TemplateColumns[i]);
            }
            headerRow.CreateCell(TemplateColumns.Length).SetCellValue("Error");

            int rowIndex = 1;
            foreach (var item in rowsWithErrors)
            {
                var row = sheet.CreateRow(rowIndex++);
                // Main data columns
                for (int c = 0; c < TemplateColumns.Length; c++)
                {
                    var key = TemplateColumns[c];
                    item.Row.TryGetValue(key, out var value);
                    row.CreateCell(c).SetCellValue(value ?? string.Empty);
                }
                // Error column
                row.CreateCell(TemplateColumns.Length).SetCellValue(item.Error ?? string.Empty);
            }

            using var ms = new MemoryStream();
            workbook.Write(ms, false);
            return ms.ToArray();
        }

        private static string? GetCellValueAsString(ICell? cell)
        {
            if (cell == null) return null;
            switch (cell.CellType)
            {
                case CellType.String:
                    return cell.StringCellValue;
                case CellType.Numeric:
                    if (DateUtil.IsCellDateFormatted(cell) && cell.DateCellValue is DateTime dt)
                        return dt.ToString("yyyy-MM-dd");
                    return cell.NumericCellValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                case CellType.Boolean:
                    return cell.BooleanCellValue.ToString();
                case CellType.Formula:
                    try
                    {
                        if (DateUtil.IsCellDateFormatted(cell))
                            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:yyyy-MM-dd}", cell.DateCellValue);
                        return cell.NumericCellValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        return cell.StringCellValue;
                    }
                default:
                    return cell.ToString();
            }
        }
    }
}
