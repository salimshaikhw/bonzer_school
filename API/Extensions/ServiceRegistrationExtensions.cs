using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Helpers.FireAndForget;
using Application.Common.Helpers.MemoryCache;
using Application.ViewModels.EmailSettings;
using Infrastructure;
using Infrastructure.Persistence.Repositories.Academics;
using Infrastructure.Persistence.Repositories.Attendances;
using Infrastructure.Persistence.Repositories.Auth;
using Infrastructure.Persistence.Repositories.Certificates;
using Infrastructure.Persistence.Repositories.Communication;
using Infrastructure.Persistence.Repositories.Exams;
using Infrastructure.Persistence.Repositories.Fees;
using Infrastructure.Persistence.Repositories.Organization;
using Infrastructure.Persistence.Repositories.Personnel;
using Infrastructure.Persistence.Repositories.Students;
using Infrastructure.Services.Academics;
using Infrastructure.Services.Administration;
using Infrastructure.Services.Attendances;
using Infrastructure.Services.Auth;
using Infrastructure.Services.Certificates;
using Infrastructure.Services.Certificates.TcWorkflow;
using Infrastructure.Services.Common;
using Infrastructure.Services.Communication;
using Infrastructure.Services.Exams;
using Infrastructure.Services.Fees;
using Infrastructure.Services.Organization;
using Infrastructure.Services.Personnel;
using Infrastructure.Services.Students;
using Microsoft.Extensions.DependencyInjection;

namespace API.Extensions
{
    public static class ServiceRegistrationExtensions
    {
        public static IServiceCollection AddEntityServices(this IServiceCollection services)
        {
            services.AddScoped<IAcademicYearService, AcademicYearService>();
            services.AddScoped<Application.Attendances.IAttendancePolicy, Infrastructure.Services.Attendances.AttendancePolicy>();
            // Attendance: write side and read side share AttendanceServiceDependencies.
            services.AddScoped<Infrastructure.Services.Attendances.AttendanceServiceDependencies>();
            services.AddScoped<IAttendanceReportService, Infrastructure.Services.Attendances.AttendanceReportService>();
            services.AddScoped<IAttendanceService, AttendanceService>();
            services.AddScoped<IAttendanceHolidayService, AttendanceHolidayService>();
            services.AddScoped<IAuditLogService, AuditLogService>();
            services.AddScoped<IBranchService, BranchService>();
            services.AddScoped<IClassFeeMapService, ClassFeeMapService>();
            // Shared reference checks of the Academics services (class/division/subject/staff exist and fit together).
            services.AddScoped<Application.Abstractions.Services.Academics.IAcademicStructure, Infrastructure.Services.Academics.AcademicStructure>();
            // Current time for business rules; replaceable in tests.
            services.AddSingleton(TimeProvider.System);
            services.AddScoped<IClassService, ClassService>();
            services.AddScoped<IClassSubjectMapService, ClassSubjectMapService>();
            services.AddScoped<IClassTeacherMapService, ClassTeacherMapService>();
            services.AddScoped<IDepartmentService, DepartmentService>();
            services.AddScoped<IDivisionService, DivisionService>();
            services.AddScoped<IExamComponentTypeService, ExamComponentTypeService>();
            services.AddScoped<IExamService, ExamService>();
            services.AddScoped<IExamSubjectComponentService, ExamSubjectComponentService>();
            services.AddScoped<IExamSubjectService, ExamSubjectService>();
            services.AddScoped<IExamTypeService, ExamTypeService>();
            services.AddScoped<IFeeHeadService, FeeHeadService>();
            services.AddScoped<IFeeInstallmentService, FeeInstallmentService>();
            services.AddScoped<IFeeStructureService, FeeStructureService>();
            services.AddScoped<IGradeMasterService, GradeMasterService>();
            services.AddScoped<IHomeworkService, HomeworkService>();
            services.AddScoped<IInstitutionTypeService, InstitutionTypeService>();
            services.AddScoped<IInvoiceService, InvoiceService>();
            services.AddScoped<Infrastructure.Services.Exams.ExamStructure>();
            services.AddScoped<IMarkService, MarkService>();
            services.AddScoped<IMediumService, MediumService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<Application.Abstractions.Services.Students.IParentAccounts, Infrastructure.Services.Students.IdentityParentAccounts>();
            services.AddScoped<IParentUserService, ParentUserService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IStaffRoleService, StaffRoleService>();
            services.AddScoped<IStaffService, StaffService>();
            services.AddScoped<IStudentResultService, StudentResultService>();
            // Student module, split by responsibility; all share StudentServiceDependencies.
            services.AddScoped<Infrastructure.Services.Students.StudentServiceDependencies>();
            services.AddScoped<IStudentQueryService, Infrastructure.Services.Students.StudentQueryService>();
            services.AddScoped<IStudentAnalyticsService, Infrastructure.Services.Students.StudentAnalyticsService>();
            services.AddScoped<IStudentReportService, Infrastructure.Services.Students.StudentReportService>();
            services.AddScoped<IStudentBulkUploadService, Infrastructure.Services.Students.StudentBulkUploadService>();
            services.AddScoped<IStudentService, StudentService>();
            services.AddScoped<IStudentSubjectService, StudentSubjectService>();
            services.AddScoped<ISubjectService, SubjectService>();
            services.AddScoped<ITeacherSubjectMapService, TeacherSubjectMapService>();
            services.AddScoped<ITimetableService, TimetableService>();
            services.AddScoped<IStudentRepository, StudentRepository>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IAcademicYearRepository, AcademicYearRepository>();
            services.AddScoped<IAttendanceRepository, AttendanceRepository>();
            services.AddScoped<ISchoolHolidayRepository, SchoolHolidayRepository>();
            services.AddScoped<IBranchRepository, BranchRepository>();
            services.AddScoped<IClassRepository, ClassRepository>();
            services.AddScoped<IDivisionRepository, DivisionRepository>();
            services.AddScoped<ISubjectRepository, SubjectRepository>();
            services.AddScoped<IMediumRepository, MediumRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<ITimetableRepository, TimetableRepository>();
            services.AddScoped<ITeacherSubjectMapRepository, TeacherSubjectMapRepository>();
            services.AddMemoryCache();
            services.AddScoped<ICacheService, CacheService>();
            services.AddScoped<IFireAndForgetService, FireAndForgetService>();
            services.AddScoped<IClassFeeMapRepository, ClassFeeMapRepository>();
            services.AddScoped<IClassSubjectMapRepository, ClassSubjectMapRepository>();
            services.AddScoped<IClassTeacherMapRepository, ClassTeacherMapRepository>();
            services.AddScoped<IDepartmentRepository, DepartmentRepository>();
            services.AddScoped<IExamRepository, ExamRepository>();
            services.AddScoped<IExamComponentTypeRepository, ExamComponentTypeRepository>();
            services.AddScoped<IExamSubjectRepository, ExamSubjectRepository>();
            services.AddScoped<IExamSubjectComponentRepository, ExamSubjectComponentRepository>();
            services.AddScoped<IExamTypeRepository, ExamTypeRepository>();
            services.AddScoped<IFeeHeadRepository, FeeHeadRepository>();
            services.AddScoped<IFeeInstallmentRepository, FeeInstallmentRepository>();
            services.AddScoped<IFeeStructureRepository, FeeStructureRepository>();
            services.AddScoped<IGradeMasterRepository, GradeMasterRepository>();
            services.AddScoped<IHomeworkRepository, HomeworkRepository>();
            services.AddScoped<IInstitutionTypeRepository, InstitutionTypeRepository>();
            services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            services.AddScoped<IMarkRepository, MarkRepository>();
            services.AddScoped<IParentUserRepository, ParentUserRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IStaffRepository, StaffRepository>();
            services.AddScoped<IStaffRoleRepository, StaffRoleRepository>();
            services.AddScoped<IStudentResultRepository, StudentResultRepository>();
            services.AddScoped<IStudentSubjectRepository, StudentSubjectRepository>();
            services.AddScoped<IMemoryCacheHelper, MemoryCacheHelper>();

            services.AddScoped<ITcCertificateRepository, TcCertificateRepository>();
            services.AddScoped<ITcCertificateService, TcCertificateService>();
            services.AddScoped<IBonafideCertificateRepository, BonafideCertificateRepository>();
            services.AddScoped<IBonafideCertificateService, BonafideCertificateService>();
            services.AddScoped<IHallTicketRepository, HallTicketRepository>();
            services.AddScoped<IHallTicketService, HallTicketService>();
            services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();
            services.AddScoped<IAttendanceCorrectionRequestRepository, AttendanceCorrectionRequestRepository>();
            services.AddScoped<IAttendanceDateLockRepository, AttendanceDateLockRepository>();
            services.AddScoped<IAttendanceAcademicYearLockRepository, AttendanceAcademicYearLockRepository>();

            services.AddScoped<IFeeStudentService, FeeStudentService>();
            services.AddScoped<IParentFeeService, ParentFeeService>();
            services.AddScoped<IFeeAnalyticsService, FeeAnalyticsService>();
            services.AddScoped<IRemainingFeesService, RemainingFeesService>();

            // Exam analytics (student, class, teacher dashboards)
            services.AddScoped<IExamAnalyticsService, ExamAnalyticsService>();

            // Design Pattern: Strategy (Behavioral) - number generators registered as separate services
            services.AddScoped<ITcNumberGenerator, TcNumberGenerator>();
            services.AddScoped<IBonafideNumberGenerator, BonafideNumberGenerator>();
            services.AddScoped<IHallTicketNumberGenerator, HallTicketNumberGenerator>();

            // Design Pattern: Chain of Responsibility (Behavioral) - TC workflow handlers
            services.AddScoped<HeadClerkApprovalHandler>();
            services.AddScoped<ClassTeacherApprovalHandler>();
            services.AddScoped<PrincipalApprovalHandler>();
            services.AddScoped<IssueTcHandler>();
            services.AddScoped<RejectTcHandler>();

            services.AddTransient<IEmailService, SmtpEmailService>();











            return services;
        }
    }
}
