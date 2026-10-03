using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Domain.Entities.Postgres.Scaffolded;
using NPOI.XSSF.UserModel;

namespace Application.Common.Helpers;

/// <summary>Pre-filled attendance import sheet for one class/division and a date range (one row per student per day).</summary>
public static class AttendanceDivisionImportTemplateExcel
{
    public static byte[] Build(
        string className,
        Guid classId,
        string divisionName,
        Guid divisionId,
        string? classTeacherName,
        IReadOnlyList<Student> studentsOrdered,
        IReadOnlyList<DateOnly> dates,
        IReadOnlyDictionary<(Guid StudentId, DateOnly Date), string> approvedLeaveTypeByStudentAndDate)
    {
        using var wb = new XSSFWorkbook();
        var sheet = wb.CreateSheet("Attendance");
        var headers = new[]
        {
            "ClassName", "ClassId", "DivisionName", "DivisionId", "ClassTeacherName",
            "RollNumber", "StudentName", "StudentId", "AttendanceDate", "Status", "LeaveType", "IsHoliday", "Remark"
        };
        var hdr = sheet.CreateRow(0);
        for (var i = 0; i < headers.Length; i++)
            hdr.CreateCell(i).SetCellValue(headers[i]);

        var rowIdx = 1;
        foreach (var student in studentsOrdered)
        {
            var name = $"{student.FirstName} {student.LastName}".Trim();
            if (string.IsNullOrEmpty(name))
                name = student.AdmissionNo ?? student.Id.ToString();

            foreach (var date in dates)
            {
                approvedLeaveTypeByStudentAndDate.TryGetValue((student.Id, date), out var leaveType);
                var row = sheet.CreateRow(rowIdx++);
                row.CreateCell(0).SetCellValue(className ?? string.Empty);
                row.CreateCell(1).SetCellValue(classId.ToString());
                row.CreateCell(2).SetCellValue(divisionName ?? string.Empty);
                row.CreateCell(3).SetCellValue(divisionId.ToString());
                row.CreateCell(4).SetCellValue(classTeacherName ?? string.Empty);
                row.CreateCell(5).SetCellValue(student.RollNumber ?? string.Empty);
                row.CreateCell(6).SetCellValue(name);
                row.CreateCell(7).SetCellValue(student.Id.ToString());
                row.CreateCell(8).SetCellValue(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                if (!string.IsNullOrEmpty(leaveType))
                {
                    row.CreateCell(9).SetCellValue("Leave");
                    row.CreateCell(10).SetCellValue(leaveType);
                }
                else
                {
                    row.CreateCell(9).SetCellValue(string.Empty);
                    row.CreateCell(10).SetCellValue(string.Empty);
                }

                row.CreateCell(11).SetCellValue(string.Empty);
                row.CreateCell(12).SetCellValue(string.Empty);
            }
        }

        for (var c = 0; c < headers.Length; c++)
            sheet.AutoSizeColumn(c);

        using var ms = new System.IO.MemoryStream();
        wb.Write(ms);
        return ms.ToArray();
    }

    public static List<DateOnly> EachDateInclusive(DateOnly start, DateOnly end)
    {
        if (end < start)
            (start, end) = (end, start);
        var list = new List<DateOnly>();
        for (var d = start; d <= end; d = d.AddDays(1))
            list.Add(d);
        return list;
    }
}
