using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Application.ViewModels.Attendance;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Application.Common.Helpers;

/// <summary>Parses bulk attendance import from .xlsx, .xls (via NPOI), or .csv into <see cref="BulkAttendanceImportRowInput"/> rows.</summary>
public static class BulkAttendanceImportFileParser
{
    private static string NormHeader(string? h)
    {
        if (string.IsNullOrWhiteSpace(h)) return string.Empty;
        var s = h.Trim().ToLowerInvariant();
        s = Regex.Replace(s, @"\s+", "");
        return s.Replace("_", "");
    }

    public static List<BulkAttendanceImportRowInput> ParseToRows(Stream stream, string? fileName)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        var ext = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
        if (ext == ".csv" || ext == ".txt")
            return ParseCsv(stream);

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        ms.Position = 0;
        return ParseExcel(ms);
    }

    public static byte[] CreateSampleWorkbookBytes()
    {
        using var wb = new XSSFWorkbook();
        var sheet = wb.CreateSheet("Attendance");
        var hdr = sheet.CreateRow(0);
        var headers = new[]
        {
            "RollNumber", "ClassId", "DivisionName", "AttendanceDate", "Status", "LeaveType", "IsHoliday"
        };
        for (var i = 0; i < headers.Length; i++)
            hdr.CreateCell(i).SetCellValue(headers[i]);

        void sampleRow(int rowIdx, string roll, string classIdPlaceholder, string div, string date, string status, string leave, string holiday)
        {
            var r = sheet.CreateRow(rowIdx);
            r.CreateCell(0).SetCellValue(roll);
            r.CreateCell(1).SetCellValue(classIdPlaceholder);
            r.CreateCell(2).SetCellValue(div);
            r.CreateCell(3).SetCellValue(date);
            r.CreateCell(4).SetCellValue(status);
            r.CreateCell(5).SetCellValue(leave);
            r.CreateCell(6).SetCellValue(holiday);
        }

        sampleRow(1, "1", "REPLACE_WITH_YOUR_CLASS_GUID", "A", "2026-03-15", "Present", "", "");
        sampleRow(2, "2", "REPLACE_WITH_YOUR_CLASS_GUID", "A", "2026-03-15", "Absent", "", "");
        sampleRow(3, "3", "REPLACE_WITH_YOUR_CLASS_GUID", "A", "2026-03-15", "Leave", "Sick", "false");

        using var outMs = new MemoryStream();
        wb.Write(outMs, leaveOpen: true);
        return outMs.ToArray();
    }

    private static List<BulkAttendanceImportRowInput> ParseExcel(MemoryStream ms)
    {
        IWorkbook workbook;
        try
        {
            workbook = WorkbookFactory.Create(ms);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Invalid Excel file. Use .xlsx or .xls.", ex);
        }

        using (workbook)
        {
            var sheet = workbook.GetSheetAt(0);
            if (sheet == null)
                throw new InvalidOperationException("The workbook has no sheets.");

            var headerRow = sheet.GetRow(sheet.FirstRowNum);
            if (headerRow == null)
                throw new InvalidOperationException("Missing header row.");

            var fmt = new DataFormatter(CultureInfo.InvariantCulture);
            var eval = workbook.GetCreationHelper().CreateFormulaEvaluator();

            var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var c = headerRow.FirstCellNum; c < headerRow.LastCellNum; c++)
            {
                var raw = GetCellString(headerRow.GetCell(c), eval, fmt);
                var key = NormHeader(raw);
                if (key.Length > 0)
                    colMap[key] = c;
            }

            int? Col(params string[] aliases)
            {
                foreach (var a in aliases)
                {
                    var n = NormHeader(a);
                    if (colMap.TryGetValue(n, out var idx))
                        return idx;
                }

                return null;
            }

            var rollC = Col("rollnumber", "roll", "rollno");
            var admC = Col("admissionno", "admission", "admno", "admissionnumber");
            var enC = Col("enrollmentno", "enrollment", "enrolment", "machineid");
            var sidC = Col("studentid", "guid", "uuid");
            var dateC = Col("attendancedate", "date", "punchdate", "logdate");
            var statusC = Col("status", "present", "state");
            var classC = Col("classid", "class");
            var divC = Col("divisionname", "divname");
            var leaveC = Col("leavetype", "leave");
            var holC = Col("isholiday", "holidayflag", "holiday");
            var remarkC = Col("remark", "note", "comments", "comment");

            if (rollC == null && admC == null && enC == null && sidC == null)
                throw new InvalidOperationException(
                    "No student column found. Add one of: RollNumber, AdmissionNo, EnrollmentNo, StudentId.");

            var rows = new List<BulkAttendanceImportRowInput>();
            for (var r = sheet.FirstRowNum + 1; r <= sheet.LastRowNum; r++)
            {
                var row = sheet.GetRow(r);
                if (row == null) continue;

                string? G(int? idx) => idx == null ? null : NullIfEmpty(GetCellString(row.GetCell(idx.Value), eval, fmt));

                var roll = G(rollC);
                var admission = G(admC);
                var enrollment = G(enC);
                var sidStr = G(sidC);
                var status = G(statusC);
                var divisionName = G(divC);
                var leaveType = G(leaveC);
                var remark = G(remarkC);
                var holStr = G(holC);

                Guid? studentId = null;
                if (!string.IsNullOrWhiteSpace(sidStr))
                    studentId = TryParseGuid(sidStr);

                Guid? classId = null;
                var classStr = G(classC);
                if (!string.IsNullOrWhiteSpace(classStr))
                    classId = TryParseGuid(classStr);

                DateOnly? dateVal = null;
                if (dateC != null)
                    dateVal = ReadDateCell(row.GetCell(dateC.Value), eval, fmt);

                var hasStudent = studentId.HasValue || !string.IsNullOrWhiteSpace(roll) ||
                                 !string.IsNullOrWhiteSpace(admission) || !string.IsNullOrWhiteSpace(enrollment);
                if (!hasStudent)
                    continue;

                var excelRowNum = r + 1;
                var input = new BulkAttendanceImportRowInput
                {
                    RowIndex = excelRowNum,
                    StudentId = studentId,
                    RollNumber = roll,
                    AdmissionNo = admission,
                    EnrollmentNo = enrollment,
                    Status = status,
                    DivisionName = divisionName,
                    LeaveType = leaveType,
                    Remark = remark,
                    ClassId = classId,
                    IsHoliday = ParseBoolOptional(holStr),
                };
                if (dateVal.HasValue)
                {
                    input.Date = dateVal;
                    input.AttendanceDate = dateVal;
                }

                rows.Add(input);
            }

            return rows;
        }
    }

    private static List<BulkAttendanceImportRowInput> ParseCsv(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var lines = new List<string[]>();
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            lines.Add(ParseCsvLine(line));
        }

        if (lines.Count < 2)
            throw new InvalidOperationException("CSV must have a header row and at least one data row.");

        var headers = lines[0].Select(NormHeader).ToArray();
        var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Length; i++)
        {
            if (headers[i].Length > 0)
                colMap[headers[i]] = i;
        }

        int? Idx(params string[] aliases)
        {
            foreach (var a in aliases)
            {
                var n = NormHeader(a);
                if (colMap.TryGetValue(n, out var idx))
                    return idx;
            }

            return null;
        }

        var rollC = Idx("rollnumber", "roll", "rollno");
        var admC = Idx("admissionno", "admission", "admno");
        var enC = Idx("enrollmentno", "enrollment", "enrolment");
        var sidC = Idx("studentid", "guid", "uuid");
        var dateC = Idx("attendancedate", "date");
        var statusC = Idx("status");
        var classC = Idx("classid", "class");
        var divC = Idx("divisionname", "divname");
        var leaveC = Idx("leavetype", "leave");
        var holC = Idx("isholiday", "holidayflag", "holiday");
        var remarkC = Idx("remark", "note", "comments");

        if (rollC == null && admC == null && enC == null && sidC == null)
            throw new InvalidOperationException(
                "No student column found. Add one of: RollNumber, AdmissionNo, EnrollmentNo, StudentId.");

        var rows = new List<BulkAttendanceImportRowInput>();
        for (var li = 1; li < lines.Count; li++)
        {
            var cells = lines[li];
            string? G(int? idx) => idx == null || idx.Value >= cells.Length ? null : NullIfEmpty(cells[idx.Value].Trim());

            var roll = G(rollC);
            var admission = G(admC);
            var enrollment = G(enC);
            var sidStr = G(sidC);
            var status = G(statusC);
            var divisionName = G(divC);
            var leaveType = G(leaveC);
            var remark = G(remarkC);
            var holStr = G(holC);

            Guid? studentId = null;
            if (!string.IsNullOrWhiteSpace(sidStr))
                studentId = TryParseGuid(sidStr);

            Guid? classId = null;
            var classStr = G(classC);
            if (!string.IsNullOrWhiteSpace(classStr))
                classId = TryParseGuid(classStr);

            DateOnly? dateVal = null;
            var dateStr = G(dateC);
            if (!string.IsNullOrWhiteSpace(dateStr) && TryParseDateOnlyLoose(dateStr, out var d))
                dateVal = d;

            var hasStudent = studentId.HasValue || !string.IsNullOrWhiteSpace(roll) ||
                             !string.IsNullOrWhiteSpace(admission) || !string.IsNullOrWhiteSpace(enrollment);
            if (!hasStudent) continue;

            var input = new BulkAttendanceImportRowInput
            {
                RowIndex = li + 1,
                StudentId = studentId,
                RollNumber = roll,
                AdmissionNo = admission,
                EnrollmentNo = enrollment,
                Status = status,
                DivisionName = divisionName,
                LeaveType = leaveType,
                Remark = remark,
                ClassId = classId,
                IsHoliday = ParseBoolOptional(holStr),
            };
            if (dateVal.HasValue)
            {
                input.Date = dateVal;
                input.AttendanceDate = dateVal;
            }

            rows.Add(input);
        }

        return rows;
    }

    private static string[] ParseCsvLine(string line)
    {
        var cells = new List<string>();
        var cur = new StringBuilder();
        var inQ = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                inQ = !inQ;
            }
            else if ((ch == ',' || ch == ';') && !inQ)
            {
                cells.Add(cur.ToString().Trim());
                cur.Clear();
            }
            else
                cur.Append(ch);
        }

        cells.Add(cur.ToString().Trim());
        return cells.ToArray();
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string? GetCellString(ICell? cell, IFormulaEvaluator eval, DataFormatter fmt)
    {
        if (cell == null) return null;
        return NullIfEmpty(fmt.FormatCellValue(cell, eval));
    }

    private static DateOnly? ReadDateCell(ICell? cell, IFormulaEvaluator eval, DataFormatter fmt)
    {
        if (cell == null) return null;
        try
        {
            if (cell.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cell))
            {
                var dt = cell.DateCellValue;
                if (dt.HasValue)
                    return DateOnly.FromDateTime(dt.Value);
            }
        }
        catch
        {
            /* fall through to string parse */
        }

        var s = fmt.FormatCellValue(cell, eval);
        if (string.IsNullOrWhiteSpace(s)) return null;
        return TryParseDateOnlyLoose(s.Trim(), out var d) ? d : null;
    }

    private static bool TryParseDateOnlyLoose(string s, out DateOnly d)
    {
        d = default;
        if (DateOnly.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
            return true;
        var m = Regex.Match(s, @"^(\d{4})-(\d{2})-(\d{2})");
        if (m.Success && DateOnly.TryParse($"{m.Groups[1].Value}-{m.Groups[2].Value}-{m.Groups[3].Value}", out d))
            return true;
        var slash = Regex.Match(s, @"^(\d{1,2})[/.-](\d{1,2})[/.-](\d{2,4})$");
        if (slash.Success)
        {
            var dd = int.Parse(slash.Groups[1].Value, CultureInfo.InvariantCulture);
            var mo = int.Parse(slash.Groups[2].Value, CultureInfo.InvariantCulture);
            var yy = int.Parse(slash.Groups[3].Value, CultureInfo.InvariantCulture);
            if (yy < 100) yy += 2000;
            try
            {
                d = new DateOnly(yy, mo, dd);
                return true;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    private static Guid? TryParseGuid(string s)
    {
        var t = s.Trim().Replace("{", "").Replace("}", "");
        if (Guid.TryParse(t, out var g)) return g;
        if (t.Length == 32 && Regex.IsMatch(t, "^[0-9a-fA-F]{32}$"))
        {
            var dashed =
                $"{t[..8]}-{t.Substring(8, 4)}-{t.Substring(12, 4)}-{t.Substring(16, 4)}-{t.Substring(20, 8)}";
            return Guid.TryParse(dashed, out var g2) ? g2 : null;
        }

        return null;
    }

    private static bool? ParseBoolOptional(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var x = s.Trim().ToLowerInvariant();
        if (x is "true" or "1" or "yes" or "y") return true;
        if (x is "false" or "0" or "no" or "n") return false;
        return null;
    }
}
