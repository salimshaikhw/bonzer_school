using Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Postgres;

/// <summary>
/// Tenant context: the school data of ONE tenant (school group). Every tenant has its own PostgreSQL
/// database with this schema; the connection string is chosen per request from the current tenant
/// (see Infrastructure.Tenancy). Shared data (tenants, logins, institution types, mediums) lives in
/// <see cref="Catalog.CatalogDbContext"/>.
///
/// Code-first. The schema is owned by the EF Core migrations in Persistence/Migrations/Tenant, which are
/// applied to every tenant database when it is provisioned and on startup.
///
/// Mapping lives on the entity classes (Domain/Entities/{Feature}) as data annotations; there are no
/// IEntityTypeConfiguration classes:
///   - standard annotations: [Table], [MaxLength], [Required], [Precision], [Index], [Column]
///   - Domain.DataAnnotations: [References] (FK without navigation), [DefaultValueSql],
///     [SoftDelete] (query filter), [FilteredIndex]; plus System.ComponentModel [DefaultValue].
///     These are applied by <see cref="DomainAnnotationsExtensions.ApplyDomainAnnotations"/>.
///
/// Naming: C# uses PascalCase; the database uses snake_case. Column, primary key, foreign key and
/// index names are derived by <see cref="SnakeCaseNamingConvention"/>.
///
/// Relationships have no navigation properties and lazy loading is not enabled, so related data is
/// only loaded through explicit joins or projections.
/// </summary>
public partial class BonzerSchoolDbContext : DbContext
{
    public BonzerSchoolDbContext(DbContextOptions<BonzerSchoolDbContext> options)
        : base(options)
    {
    }

    // Organization
    public virtual DbSet<Branch> Branches { get; set; }
    public virtual DbSet<AcademicYear> AcademicYears { get; set; }
    public virtual DbSet<SchoolHoliday> SchoolHolidays { get; set; }
    public virtual DbSet<Department> Departments { get; set; }

    // Academic
    public virtual DbSet<Class> Classes { get; set; }
    public virtual DbSet<Division> Divisions { get; set; }
    public virtual DbSet<Subject> Subjects { get; set; }
    public virtual DbSet<ClassSubjectMap> ClassSubjectMaps { get; set; }
    public virtual DbSet<ClassTeacherMap> ClassTeacherMaps { get; set; }
    public virtual DbSet<TeacherSubjectMap> TeacherSubjectMaps { get; set; }
    public virtual DbSet<Timetable> Timetables { get; set; }
    public virtual DbSet<Homework> Homeworks { get; set; }

    // Students
    public virtual DbSet<Student> Students { get; set; }
    public virtual DbSet<StudentSubject> StudentSubjects { get; set; }
    public virtual DbSet<ParentUser> ParentUsers { get; set; }
    public virtual DbSet<LeaveRequest> LeaveRequests { get; set; }

    // Attendance
    public virtual DbSet<Attendance> Attendances { get; set; }
    public virtual DbSet<AttendanceDateLock> AttendanceDateLocks { get; set; }
    public virtual DbSet<AttendanceAcademicYearLock> AttendanceAcademicYearLocks { get; set; }
    public virtual DbSet<AttendanceCorrectionRequest> AttendanceCorrectionRequests { get; set; }

    // Exams
    public virtual DbSet<ExamType> ExamTypes { get; set; }
    public virtual DbSet<Exam> Exams { get; set; }
    public virtual DbSet<ExamComponentType> ExamComponentTypes { get; set; }
    public virtual DbSet<ExamSubject> ExamSubjects { get; set; }
    public virtual DbSet<ExamSubjectComponent> ExamSubjectComponents { get; set; }
    public virtual DbSet<Mark> Marks { get; set; }
    public virtual DbSet<StudentResult> StudentResults { get; set; }
    public virtual DbSet<GradeMaster> GradeMasters { get; set; }
    public virtual DbSet<HallTicket> HallTickets { get; set; }

    // Fees
    public virtual DbSet<FeeHead> FeeHeads { get; set; }
    public virtual DbSet<FeeInstallment> FeeInstallments { get; set; }
    public virtual DbSet<FeeStructure> FeeStructures { get; set; }
    public virtual DbSet<ClassFeeMap> ClassFeeMaps { get; set; }
    public virtual DbSet<Invoice> Invoices { get; set; }
    public virtual DbSet<InvoiceItem> InvoiceItems { get; set; }
    public virtual DbSet<Payment> Payments { get; set; }
    public virtual DbSet<PaymentFeeLine> PaymentFeeLines { get; set; }

    // Staff and legacy (non-Identity) users. Logins are ASP.NET Identity users in the catalog database.
    public virtual DbSet<Staff> Staff { get; set; }
    public virtual DbSet<StaffRole> StaffRoles { get; set; }
    public virtual DbSet<Role> SchoolRoles { get; set; }
    public virtual DbSet<User> SchoolUsers { get; set; }

    // Certificates
    public virtual DbSet<TcCertificate> TcCertificates { get; set; }
    public virtual DbSet<BonafideCertificate> BonafideCertificates { get; set; }
    public virtual DbSet<StudentIdCard> StudentIdCards { get; set; }
    public virtual DbSet<TcClearance> TcClearances { get; set; }
    public virtual DbSet<TcWorkflowLog> TcWorkflowLogs { get; set; }
    /// <summary>Table "departments": TC no-dues departments (not the staff table "department").</summary>
    public virtual DbSet<ClearanceDepartment> ClearanceDepartments { get; set; }

    // System
    public virtual DbSet<AuditLog> AuditLogs { get; set; }
    public virtual DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyDomainAnnotations();

        OnModelCreatingPartial(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // Program.cs enables Npgsql legacy timestamp behaviour (any DateTime.Kind is accepted), which
        // pairs with "timestamp without time zone". Stated here so migrations never depend on that switch.
        configurationBuilder.Properties<DateTime>().HaveColumnType("timestamp without time zone");
        configurationBuilder.Properties<DateTime?>().HaveColumnType("timestamp without time zone");

        configurationBuilder.Conventions.Add(_ => new SnakeCaseNamingConvention());
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
