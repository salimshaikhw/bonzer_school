using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Helpers.FireAndForget;
using Application.Common.Helpers.MemoryCache;
using Application.ViewModels.EmailSettings;
using Infrastructure;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Services;
using Infrastructure.Services.NumberGenerators;
using Infrastructure.Services.TcWorkflow;
using Microsoft.Extensions.DependencyInjection;

namespace API.Extensions
{
    public static class ServiceRegistrationExtensions
    {
        public static IServiceCollection AddEntityServices(this IServiceCollection services)
        {
            services.AddScoped<IAcademicYearService, AcademicYearService>();
            services.AddScoped<IAttendanceService, AttendanceService>();
            services.AddScoped<IAttendanceHolidayService, AttendanceHolidayService>();
            services.AddScoped<IAuditLogService, AuditLogService>();
            services.AddScoped<IBranchService, BranchService>();
            services.AddScoped<IClassFeeMapService, ClassFeeMapService>();
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
            services.AddScoped<IMarkService, MarkService>();
            services.AddScoped<IMediumService, MediumService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IParentUserService, ParentUserService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IStaffRoleService, StaffRoleService>();
            services.AddScoped<IStaffService, StaffService>();
            services.AddScoped<IStudentResultService, StudentResultService>();
            services.AddScoped<IStudentService, StudentService>();
            services.AddScoped<IStudentSubjectService, StudentSubjectService>();
            services.AddScoped<ISubjectService, SubjectService>();
            services.AddScoped<ITeacherSubjectMapService, TeacherSubjectMapService>();
            services.AddScoped<ITimetableService, TimetableService>();
            services.AddScoped<IStudentRepository, StudentRepository>();
            services.AddScoped<IStudentService, StudentService>();
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
            services.AddScoped<ISchoolGroupRepository, SchoolGroupRepository>();
            services.AddScoped<ITimetableRepository, TimetableRepository>();
            services.AddScoped<ITeacherSubjectMapRepository, TeacherSubjectMapRepository>();
            services.AddMemoryCache();
            services.AddScoped<ICacheService, CacheService>();
            services.AddScoped<IFireAndForgetService, FireAndForgetService>();
            services.AddScoped<IClassFeeMapRepository, ClassFeeMapRepository>();
            services.AddScoped<IClassSubjectMapRepository, ClassSubjectMapRepository>();
            services.AddScoped<IClassTeacherMapRepository, ClassTeacherMapRepository>();
            services.AddScoped<IClassTeacherMapService, ClassTeacherMapService>();
            services.AddScoped<IDepartmentRepository, DepartmentRepository>();
            services.AddScoped<IDepartmentService, DepartmentService>();
            services.AddScoped<IDivisionService, DivisionService>();
            services.AddScoped<IExamRepository, ExamRepository>();
            services.AddScoped<IExamService, ExamService>();
            services.AddScoped<IExamComponentTypeRepository, ExamComponentTypeRepository>();
            services.AddScoped<IExamComponentTypeService, ExamComponentTypeService>();
            services.AddScoped<IExamSubjectRepository, ExamSubjectRepository>();
            services.AddScoped<IExamSubjectService, ExamSubjectService>();
            services.AddScoped<IExamSubjectComponentRepository, ExamSubjectComponentRepository>();
            services.AddScoped<IExamSubjectComponentService, ExamSubjectComponentService>();
            services.AddScoped<IExamTypeRepository, ExamTypeRepository>();
            services.AddScoped<IExamTypeService, ExamTypeService>();
            services.AddScoped<IFeeHeadRepository, FeeHeadRepository>();
            services.AddScoped<IFeeHeadService, FeeHeadService>();
            services.AddScoped<IFeeInstallmentRepository, FeeInstallmentRepository>();
            services.AddScoped<IFeeInstallmentService, FeeInstallmentService>();
            services.AddScoped<IFeeStructureRepository, FeeStructureRepository>();
            services.AddScoped<IFeeStructureService, FeeStructureService>();
            services.AddScoped<IGradeMasterRepository, GradeMasterRepository>();
            services.AddScoped<IGradeMasterService, GradeMasterService>();
            services.AddScoped<IHomeworkRepository, HomeworkRepository>();
            services.AddScoped<IHomeworkService, HomeworkService>();
            services.AddScoped<IInstitutionTypeRepository, InstitutionTypeRepository>();
            services.AddScoped<IInstitutionTypeService, InstitutionTypeService>();
            services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            services.AddScoped<IInvoiceService, InvoiceService>();
            services.AddScoped<IMarkRepository, MarkRepository>();
            services.AddScoped<IMarkService, MarkService>();
            services.AddScoped<IMediumRepository, MediumRepository>();
            services.AddScoped<IMediumService, MediumService>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IParentUserRepository, ParentUserRepository>();
            services.AddScoped<IParentUserService, ParentUserService>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IStaffRepository, StaffRepository>();
            services.AddScoped<IStaffService, StaffService>();
            services.AddScoped<IStaffRoleRepository, StaffRoleRepository>();
            services.AddScoped<IStaffRoleService, StaffRoleService>();
            services.AddScoped<IStudentResultRepository, StudentResultRepository>();
            services.AddScoped<IStudentResultService, StudentResultService>();
            services.AddScoped<IStudentSubjectRepository, StudentSubjectRepository>();
            services.AddScoped<IStudentSubjectService, StudentSubjectService>();
            services.AddScoped<ISubjectRepository, SubjectRepository>();
            services.AddScoped<ISubjectService, SubjectService>();
            services.AddScoped<ITeacherSubjectMapRepository, TeacherSubjectMapRepository>();
            services.AddScoped<ITeacherSubjectMapService, TeacherSubjectMapService>();
            services.AddScoped<ITimetableRepository, TimetableRepository>();
            services.AddScoped<ITimetableService, TimetableService>();
            services.AddScoped<ISchoolGroupRepository, SchoolGroupRepository>();
            services.AddScoped<ISchoolGroupService, SchoolGroupService>();
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
