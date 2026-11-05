using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace API.Swagger
{
    /// <summary>
    /// Orders the Swagger sections the way the application is set up and used, instead of alphabetically,
    /// and prefixes each with its step number. Controllers not listed here follow, alphabetically.
    /// </summary>
    public sealed class FlowOrderDocumentFilter : IDocumentFilter
    {
        // Controller name without the "Controller" suffix, in flow order.
        private static readonly string[] Flow =
        {
            // 1. Sign in and tenant
            "Auth", "Connect", "OAuthClients", "Role", "User",
            // 2. School structure
            "SchoolGroup", "InstitutionType", "Medium", "Branch", "AcademicYear",
            // 3. Academics setup
            "Class", "Division", "Subject", "ClassSubjectMap",
            // 4. Staff
            "Department", "StaffRole", "Staff", "TeacherSubjectMap", "ClassTeacherMap",
            // 5. Students
            "Student", "StudentSubject", "ParentUser",
            // 6. Timetable and homework
            "Timetable", "Homework",
            // 7. Attendance
            "AttendanceHoliday", "Attendance", "AttendanceOperations", "AttendanceAnalytics",
            // 8. Fees
            "FeeHead", "FeeStructure", "FeeInstallment", "ClassFeeMap", "Invoice", "Payment", "Fees", "RemainingFees", "ParentFees",
            // 9. Exams
            "ExamType", "ExamComponentType", "GradeMaster", "Exam", "ExamSubject", "ExamSubjectComponent", "HallTicket", "Mark", "StudentResult", "ExamAnalytics",
            // 10. Certificates
            "BonafideCertificate", "TcCertificate",
            // 11. Communication and administration
            "Notification", "Emails", "AuditLog", "ApiDocumentation",
        };

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var used = swaggerDoc.Paths.Values
                .SelectMany(p => p.Operations.Values)
                .SelectMany(o => o.Tags)
                .Select(t => t.Name)
                .Distinct()
                .ToList();

            var ordered = Flow.Where(used.Contains)
                .Concat(used.Except(Flow).OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
                .ToList();

            swaggerDoc.Tags = ordered
                .Select((name, i) => new OpenApiTag { Name = name, Description = $"Step {i + 1}" })
                .ToList();
        }
    }
}
