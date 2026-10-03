using System;
using System.Collections.Generic;
using Domain.Entities.Postgres.Scaffolded;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Infrastructure.Security.Entities;
using Microsoft.AspNetCore.Identity;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace Infrastructure.Persistence.Postgres;

public partial class BonzerSchoolDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public BonzerSchoolDbContext(DbContextOptions<BonzerSchoolDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AcademicYear> academic_years { get; set; }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<AttendanceDateLock> attendance_date_locks { get; set; }

    public virtual DbSet<AttendanceAcademicYearLock> attendance_academic_year_locks { get; set; }

    public virtual DbSet<AuditLog> audit_logs { get; set; }

    public virtual DbSet<Branch> branches { get; set; }

    public virtual DbSet<SchoolHoliday> school_holidays { get; set; }

    public virtual DbSet<Class> classes { get; set; }

    public virtual DbSet<ClassFeeMap> class_fee_maps { get; set; }

    public virtual DbSet<ClassSubjectMap> class_subject_maps { get; set; }

    public virtual DbSet<ClassTeacherMap> class_teacher_maps { get; set; }

    public virtual DbSet<Department> departments { get; set; }

    public virtual DbSet<Division> divisions { get; set; }

    public virtual DbSet<Exam> exams { get; set; }

    public virtual DbSet<ExamComponentType> exam_component_types { get; set; }

    public virtual DbSet<ExamSubject> exam_subjects { get; set; }

    public virtual DbSet<ExamSubjectComponent> exam_subject_components { get; set; }

    public virtual DbSet<ExamType> exam_types { get; set; }

    public virtual DbSet<FeeHead> fee_heads { get; set; }

    public virtual DbSet<FeeInstallment> fee_installments { get; set; }

    public virtual DbSet<FeeStructure> fee_structures { get; set; }

    public virtual DbSet<GradeMaster> grade_masters { get; set; }

    public virtual DbSet<Homework> homeworks { get; set; }

    public virtual DbSet<InstitutionType> institution_types { get; set; }

    public virtual DbSet<Invoice> invoices { get; set; }

    public virtual DbSet<InvoiceItem> invoice_items { get; set; }

    public virtual DbSet<Mark> marks { get; set; }

    public virtual DbSet<Medium> mediums { get; set; }

    public virtual DbSet<Notification> notifications { get; set; }

    public virtual DbSet<ParentUser> parent_users { get; set; }

    public virtual DbSet<Payment> payments { get; set; }

    public virtual DbSet<PaymentFeeLine> payment_fee_lines { get; set; }

    public virtual DbSet<Role> roles { get; set; }

    public virtual DbSet<SchoolGroup> school_groups { get; set; }

    public virtual DbSet<Staff> staff { get; set; }

    public virtual DbSet<StaffRole> staff_roles { get; set; }

    public virtual DbSet<Student> students { get; set; }

    public virtual DbSet<StudentResult> student_results { get; set; }

    public virtual DbSet<StudentSubject> student_subjects { get; set; }

    public virtual DbSet<Subject> subjects { get; set; }

    public virtual DbSet<TeacherSubjectMap> teacher_subject_maps { get; set; }

    public virtual DbSet<Timetable> timetables { get; set; }

    public virtual DbSet<User> users { get; set; }

    public virtual DbSet<TcCertificate> tc_certificates { get; set; }

    public virtual DbSet<BonafideCertificate> bonafide_certificates { get; set; }

    public virtual DbSet<HallTicket> hall_tickets { get; set; }

    public virtual DbSet<LeaveRequest> leave_requests { get; set; }

    public virtual DbSet<AttendanceCorrectionRequest> attendance_correction_requests { get; set; }

    public virtual DbSet<Infrastructure.Security.Entities.RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
   

        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("uuid-ossp");


        modelBuilder.Entity<AcademicYear>(entity =>
        {
            entity.ToTable("academic_years");

            entity.HasKey(e => e.Id).HasName("academic_years_pkey");

            entity.Property(e => e.Id)
                  .HasColumnName("id")
                  .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.YearName)
                  .HasColumnName("year_name")
                  .HasMaxLength(20);

            entity.Property(e => e.StartDate)
                  .HasColumnName("start_date");

            entity.Property(e => e.EndDate)
                  .HasColumnName("end_date");

            entity.Property(e => e.IsActive)
                  .HasColumnName("is_active")
                  .HasDefaultValue(true);
        });


        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.ToTable("attendance");

            entity.HasKey(e => e.Id).HasName("attendance_pkey");

            entity.HasIndex(e => e.AttendanceDate, "idx_attendance_date");
            entity.HasIndex(e => e.DivisionId, "idx_attendance_division");
            entity.HasIndex(e => new { e.StudentId, e.AttendanceDate }, "unique_attendance").IsUnique();

            entity.Property(e => e.Id)
                  .HasColumnName("id")
                  .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.ClassTeacherId).HasColumnName("classteacher_id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.DivisionId).HasColumnName("division_id");

            entity.Property(e => e.AttendanceDate).HasColumnName("attendance_date");

            entity.Property(e => e.Status)
                  .HasColumnName("status")
                  .HasMaxLength(20);

            entity.Property(e => e.LeaveType)
                  .HasColumnName("leave_type")
                  .HasMaxLength(50);

            entity.Property(e => e.IsHoliday)
                  .HasColumnName("is_holiday")
                  .HasDefaultValue(false);

            entity.Property(e => e.Remark).HasColumnName("remark");

            entity.Property(e => e.CreatedAt)
                  .HasColumnName("created_at")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP")
                  .HasColumnType("timestamp without time zone");

            entity.Property(e => e.IsDeleted)
                  .HasColumnName("is_deleted")
                  .HasDefaultValue(false);

            entity.Property(e => e.DeletedAt)
                  .HasColumnName("deleted_at")
                  .HasColumnType("timestamp without time zone");
        });


        modelBuilder.Entity<AttendanceDateLock>(entity =>
        {
            entity.ToTable("attendance_date_locks");

            entity.HasKey(e => e.Id).HasName("attendance_date_locks_pkey");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.DivisionId).HasColumnName("division_id");
            entity.Property(e => e.AttendanceDate).HasColumnName("attendance_date");

            entity.Property(e => e.IsLocked).HasColumnName("is_locked");

            entity.Property(e => e.LockedAt)
                .HasColumnName("locked_at")
                .HasColumnType("timestamp without time zone");

            entity.Property(e => e.UnlockedAt)
                .HasColumnName("unlocked_at")
                .HasColumnType("timestamp without time zone");
        });

        modelBuilder.Entity<AttendanceAcademicYearLock>(entity =>
        {
            entity.ToTable("attendance_academic_year_locks");

            entity.HasKey(e => e.Id).HasName("attendance_academic_year_locks_pkey");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.AcademicYearId).HasColumnName("academic_year_id");

            entity.Property(e => e.IsLocked).HasColumnName("is_locked");

            entity.Property(e => e.LockedAt)
                .HasColumnName("locked_at")
                .HasColumnType("timestamp without time zone");

            entity.Property(e => e.UnlockedAt)
                .HasColumnName("unlocked_at")
                .HasColumnType("timestamp without time zone");
        });
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");

            entity.HasKey(e => e.id).HasName("audit_logs_pkey");

            entity.Property(e => e.id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.branch_id).HasColumnName("branch_id");
            entity.Property(e => e.user_id).HasColumnName("user_id");

            entity.Property(e => e.module)
                .HasColumnName("module")
                .HasMaxLength(50);

            entity.Property(e => e.action_type)
                .HasColumnName("action_type")
                .HasMaxLength(50);

            entity.Property(e => e.record_id).HasColumnName("record_id");
            entity.Property(e => e.description).HasColumnName("description");

            entity.Property(e => e.created_at)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
        });

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.ToTable("branches"); // Ensure you are mapping to the correct table name

            entity.HasKey(e => e.Id).HasName("branches_pkey");

            // ADD THIS LINE: Explicitly map 'Id' to the database column 'id'
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

            // Ensure all other properties are mapped to their snake_case equivalents
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.InstitutionTypeId).HasColumnName("institution_type_id");
            entity.Property(e => e.MediumId).HasColumnName("medium_id");
            entity.Property(e => e.Schooluid).HasColumnName("school_uid");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.City).HasColumnName("city");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.CreatedAt)
                  .HasColumnName("created_at") // Important: Map this too
                  .HasDefaultValueSql("CURRENT_TIMESTAMP")
                  .HasColumnType("timestamp without time zone");

            entity.Property(e => e.AttendanceSaturdayOff).HasColumnName("attendance_saturday_off");
            entity.Property(e => e.AttendanceSaturdayPattern)
                  .HasColumnName("attendance_saturday_pattern")
                  .HasConversion<short>();
            entity.Property(e => e.AttendanceSundayOff).HasColumnName("attendance_sunday_off");
        });

        modelBuilder.Entity<SchoolHoliday>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("school_holidays_pkey");
            entity.ToTable("school_holidays");
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.HolidayDate).HasColumnName("holiday_date");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200);
            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
            entity.HasIndex(e => new { e.BranchId, e.HolidayDate }, "uq_school_holidays_branch_date_active")
                .IsUnique()
                .HasFilter("is_deleted IS NOT TRUE");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.ToTable("classes");

            entity.HasKey(e => e.Id).HasName("classes_pkey");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.BranchId)
                .HasColumnName("branch_id");

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(50);
        });

        modelBuilder.Entity<ClassFeeMap>(entity =>
        {
            entity.ToTable("class_fee_map");
            entity.HasKey(e => e.Id).HasName("class_fee_map_pkey");

            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.DivisionId).HasColumnName("division_id");
            entity.Property(e => e.AcademicYearId).HasColumnName("academic_year_id");
            entity.Property(e => e.FeeHeadId).HasColumnName("fee_head_id");
            entity.Property(e => e.Amount).HasColumnName("amount").HasPrecision(10, 2);
            entity.Property(e => e.InstallmentNo).HasColumnName("installment_no");
            entity.Property(e => e.AcademicYear).HasColumnName("academic_year").HasMaxLength(20);
            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
        });

        modelBuilder.Entity<ClassSubjectMap>(entity =>
        {
            entity.ToTable("class_subject_map");

            entity.HasKey(e => e.Id).HasName("class_subject_map_pkey");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.DivisionId).HasColumnName("division_id");
            //entity.Property(e => e.SectionId).HasColumnName("section_id");  

            entity.Property(e => e.SubjectId).HasColumnName("subject_id");

            entity.Property(e => e.AcademicYear)
                .HasColumnName("academic_year")
                .HasMaxLength(20);
        });

        modelBuilder.Entity<ClassTeacherMap>(entity =>
        {
            entity.ToTable("class_teacher_map");

            entity.HasKey(e => e.Id).HasName("class_teacher_map_pkey");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.DivisionId).HasColumnName("division_id");
            //entity.Property(e => e.SectionId).HasColumnName("section_id");  
            entity.Property(e => e.SubjectId).HasColumnName("subject_id");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");

            entity.Property(e => e.AcademicYear)
                .HasColumnName("academic_year")
                .HasMaxLength(20);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("department");

            entity.HasKey(e => e.Id).HasName("department_pkey");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(20);
        });

        modelBuilder.Entity<Division>(entity =>
        {
            entity.ToTable("divisions");

            entity.HasKey(e => e.Id).HasName("divisions_pkey");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.ClassId)
                .HasColumnName("class_id");

            entity.Property(e => e.BranchId)
                .HasColumnName("branch_id");

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(100);

            entity.Property(e => e.Code)
                .HasColumnName("code")
                .HasMaxLength(50);

            entity.Property(e => e.AvailableSeats)
                .HasColumnName("available_seats");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
        });

        modelBuilder.Entity<Exam>(entity =>
        {
            entity.ToTable("exams");

            entity.HasKey(e => e.Id).HasName("exams_pkey");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(100);

            entity.Property(e => e.ExamTypeId).HasColumnName("exam_type_id");
            entity.Property(e => e.AcademicYearId).HasColumnName("academic_year_id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.DivisionId).HasColumnName("division_id");
            //entity.Property(e => e.SectionId).HasColumnName("section_id"); 

            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.EndDate).HasColumnName("end_date");

            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(20);

            entity.Property(e => e.IsLocked)
                .HasColumnName("is_locked")
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
        });

        modelBuilder.Entity<ExamComponentType>(entity =>
        {
            entity.ToTable("exam_component_types");

            entity.HasKey(e => e.Id).HasName("exam_component_types_pkey");

            entity.HasIndex(e => e.Name, "exam_component_types_name_key").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Code)
                .HasColumnName("code")
                .HasMaxLength(20);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
        });

        modelBuilder.Entity<ExamSubject>(entity =>
        {
            entity.ToTable("exam_subjects");

            entity.HasKey(e => e.Id).HasName("exam_subjects_pkey");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.ExamId).HasColumnName("exam_id");
            entity.Property(e => e.SubjectId).HasColumnName("subject_id");

            entity.Property(e => e.TotalMaxMarks)
                .HasColumnName("total_max_marks")
                .HasPrecision(6, 2);

            entity.Property(e => e.TotalPassMarks)
                .HasColumnName("total_pass_marks")
                .HasPrecision(6, 2);

            entity.Property(e => e.ExamDate).HasColumnName("exam_date");

            entity.Property(e => e.StartTime)
                .HasColumnName("start_time")
                .HasColumnType("time without time zone");

            entity.Property(e => e.EndTime)
                .HasColumnName("end_time")
                .HasColumnType("time without time zone");

            entity.Property(e => e.DurationMinutes).HasColumnName("duration_minutes");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
        });
        modelBuilder.Entity<ExamSubjectComponent>(entity =>
        {
            entity.ToTable("exam_subject_components");

            entity.HasKey(e => e.Id).HasName("exam_subject_components_pkey");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.ExamSubjectId).HasColumnName("exam_subject_id");
            entity.Property(e => e.ComponentTypeId).HasColumnName("component_type_id");

            entity.Property(e => e.MaxMarks)
                .HasColumnName("max_marks")
                .HasPrecision(5, 2);

            entity.Property(e => e.PassMarks)
                .HasColumnName("pass_marks")
                .HasPrecision(5, 2);

            entity.Property(e => e.WeightagePercent)
                .HasColumnName("weightage_percent")
                .HasPrecision(5, 2);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
        });

        modelBuilder.Entity<ExamType>(entity =>
        {
            entity.ToTable("exam_types");

            entity.HasKey(e => e.Id).HasName("exam_types_pkey");

            entity.HasIndex(e => e.Name, "exam_types_name_key").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(50);

            entity.Property(e => e.Description)
                .HasColumnName("description");
        });


        modelBuilder.Entity<FeeHead>(entity =>
        {
            entity.ToTable("fee_heads");

            entity.HasKey(e => e.Id).HasName("fee_heads_pkey");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(100);

            entity.Property(e => e.AccountCode)
                .HasColumnName("account_code")
                .HasMaxLength(50);
        });

        modelBuilder.Entity<FeeInstallment>(entity =>
        {
            entity.ToTable("fee_installments");

            entity.HasKey(e => e.Id).HasName("fee_installments_pkey");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.AcademicYearId).HasColumnName("academic_year_id");

            entity.Property(e => e.InstallmentNo).HasColumnName("installment_no");

            entity.Property(e => e.InstallmentName)
                .HasColumnName("installment_name")
                .HasMaxLength(50);

            entity.Property(e => e.DueDate).HasColumnName("due_date");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");

            entity.Property(e => e.GraceDays)
                .HasColumnName("grace_days")
                .HasDefaultValue(0);

            entity.Property(e => e.LateFeePerDay)
                .HasColumnName("late_fee_per_day")
                .HasPrecision(10, 2)
                .HasDefaultValue(0.00m);

            entity.Property(e => e.LateFeeAfter).HasColumnName("late_fee_after");
        });

        modelBuilder.Entity<FeeStructure>(entity =>
        {
            entity.ToTable("fee_structures");

            entity.HasKey(e => e.Id).HasName("fee_structures_pkey");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.DivisionId).HasColumnName("division_id");
            entity.Property(e => e.FeeHeadId).HasColumnName("fee_head_id");

            entity.Property(e => e.Amount)
                .HasColumnName("amount")
                .HasPrecision(10, 2);
        });

        modelBuilder.Entity<GradeMaster>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("grade_master_pkey");

            entity.ToTable("grade_master");

            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.Grade).HasMaxLength(5);
            entity.Property(e => e.MaxPercent).HasPrecision(5, 2);
            entity.Property(e => e.MinPercent).HasPrecision(5, 2);
        });

        modelBuilder.Entity<Homework>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("homework_pkey");

            entity.ToTable("homework");

            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
        });

        modelBuilder.Entity<InstitutionType>(entity =>
        {
            entity.ToTable("institution_types");

            entity.HasKey(e => e.Id).HasName("institution_types_pkey");

            entity.HasIndex(e => e.Name, "institution_types_name_key").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(50);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("invoices_pkey");

            entity.HasIndex(e => e.DueDate, "idx_invoice_due");

            entity.HasIndex(e => e.StudentId, "idx_invoice_student");

            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.InvoiceNo).HasMaxLength(50);
            entity.Property(e => e.PaidAmount)
                .HasPrecision(10, 2)
                .HasDefaultValueSql("0");
            entity.Property(e => e.RemainingAmount).HasPrecision(10, 2);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.TotalAmount).HasPrecision(10, 2);
        });

        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("invoice_items_pkey");

            entity.ToTable("invoice_items");

            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.TotalAmount).HasPrecision(10, 2);
            entity.Property(e => e.PaidAmount).HasPrecision(10, 2);
            entity.Property(e => e.RemainingAmount).HasPrecision(10, 2);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
        });

        modelBuilder.Entity<Mark>(entity =>
        {
            entity.ToTable("marks");

            entity.HasKey(e => e.Id).HasName("marks_pkey");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.ExamSubjectComponentId).HasColumnName("exam_subject_component_id");
            entity.Property(e => e.StudentId).HasColumnName("student_id");

            entity.Property(e => e.MarksObtained)
                .HasColumnName("marks_obtained")
                .HasPrecision(6, 2);

            entity.Property(e => e.IsAbsent)
                .HasColumnName("is_absent")
                .HasDefaultValue(false);

            entity.Property(e => e.IsMedical)
                .HasColumnName("is_medical")
                .HasDefaultValue(false);

            entity.Property(e => e.IsExempt)
                .HasColumnName("is_exempt")
                .HasDefaultValue(false);

            entity.Property(e => e.Remark).HasColumnName("remark");
            entity.Property(e => e.EnteredBy).HasColumnName("entered_by");

            entity.Property(e => e.EnteredAt)
                .HasColumnName("entered_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
        });

        modelBuilder.Entity<Medium>(entity =>
        {
            entity.ToTable("mediums");

            entity.HasKey(e => e.Id).HasName("mediums_pkey");

            entity.HasIndex(e => e.Name, "mediums_name_key").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(50);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notifications_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Type).HasMaxLength(50);
        });

        modelBuilder.Entity<ParentUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("parent_users_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Username).HasMaxLength(100);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");

            entity.HasKey(e => e.Id).HasName("payments_pkey");

            entity.HasIndex(e => e.CollectedByStaffId, "idx_payment_accountant");
            entity.HasIndex(e => e.DivisionId, "idx_payment_division");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.InvoiceId)
                .HasColumnName("invoice_id")
                .HasMaxLength(50);

            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.DivisionId).HasColumnName("division_id");
            entity.Property(e => e.CollectedByStaffId).HasColumnName("collected_by_staff_id");
            entity.Property(e => e.PaymentDate).HasColumnName("payment_date");

            entity.Property(e => e.Amount)
                .HasColumnName("amount")
                .HasPrecision(10, 2);

            entity.Property(e => e.PaymentMethod)
                .HasColumnName("payment_method")
                .HasMaxLength(50);

            entity.Property(e => e.PaymentType)
                .HasColumnName("payment_type")
                .HasMaxLength(50);

            entity.Property(e => e.TransactionNumber)
                .HasColumnName("transaction_number")
                .HasMaxLength(100);

            entity.Property(e => e.BankName)
                .HasColumnName("bank_name")
                .HasMaxLength(100);

            entity.Property(e => e.Remarks).HasColumnName("remarks");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");

            entity.Property(e => e.InstallmentId).HasColumnName("installment_id"); 
        });

        modelBuilder.Entity<PaymentFeeLine>(entity =>
        {
            entity.ToTable("payment_fee_lines");

            entity.HasKey(e => e.Id).HasName("payment_fee_lines_pkey");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.PaymentId).HasColumnName("payment_id");

            entity.Property(e => e.FeeHeadId)
                .HasColumnName("fee_head_id")
                .IsRequired(false);

            entity.Property(e => e.Amount)
                .HasColumnName("amount")
                .HasPrecision(10, 2);

            entity.Property(e => e.CustomFeeName)
                .HasColumnName("custom_fee_name")
                .HasMaxLength(255);

 
            entity.HasOne<Payment>()
                .WithMany()
                .HasForeignKey(e => e.PaymentId)
                .HasConstraintName("payment_fee_lines_payment_id_fkey")
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne<FeeHead>()
                .WithMany()
                .HasForeignKey(e => e.FeeHeadId)
                .HasConstraintName("payment_fee_lines_fee_head_id_fkey")
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");

            entity.HasKey(e => e.Id).HasName("roles_pkey");

            entity.HasIndex(e => e.Name, "roles_name_key").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(50);

            entity.Property(e => e.Description)
                .HasColumnName("description");
        });

        modelBuilder.Entity<SchoolGroup>(entity =>
        {
            entity.ToTable("school_groups");

            entity.HasKey(e => e.Id).HasName("school_groups_pkey");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(200);

            entity.Property(e => e.Code)
                .HasColumnName("code")
                .HasMaxLength(50);

            entity.Property(e => e.Address)
                .HasColumnName("address");

            entity.Property(e => e.Phone)
                .HasColumnName("phone")
                .HasMaxLength(20);

            entity.Property(e => e.Email)
                .HasColumnName("email")
                .HasMaxLength(150);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.ToTable("staff");

            entity.HasKey(e => e.Id).HasName("staff_pkey");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.DeptId).HasColumnName("dept_id");

            entity.Property(e => e.StaffCode)
                .HasColumnName("staff_code")
                .HasMaxLength(50);

            entity.Property(e => e.Designation)
                .HasColumnName("designation")
                .HasMaxLength(100);

            entity.Property(e => e.FirstName)
                .HasColumnName("first_name")
                .HasMaxLength(100);

            entity.Property(e => e.MiddleName)
                .HasColumnName("middle_name")
                .HasMaxLength(100);

            entity.Property(e => e.LastName)
                .HasColumnName("last_name")
                .HasMaxLength(100);

            entity.Property(e => e.Gender)
                .HasColumnName("gender")
                .HasMaxLength(20);

            entity.Property(e => e.Dob).HasColumnName("dob");

            entity.Property(e => e.Phone)
                .HasColumnName("phone")
                .HasMaxLength(20);

            entity.Property(e => e.Email)
                .HasColumnName("email")
                .HasMaxLength(150);

            entity.Property(e => e.Address).HasColumnName("address");

            entity.Property(e => e.City)
                .HasColumnName("city")
                .HasMaxLength(100);

            entity.Property(e => e.State)
                .HasColumnName("state")
                .HasMaxLength(100);

            entity.Property(e => e.JoiningDate).HasColumnName("joining_date");

            entity.Property(e => e.Salary)
                .HasColumnName("salary")
                .HasPrecision(12, 2);

            entity.Property(e => e.Qualification)
                .HasColumnName("qualification")
                .HasMaxLength(50);

            entity.Property(e => e.Qualificationdetails)
                .HasColumnName("qualification_details")
                .HasMaxLength(200);

            entity.Property(e => e.PreviousEmployer)
                .HasColumnName("previous_employer")
                .HasMaxLength(200);

            entity.Property(e => e.YearsOfExperience).HasColumnName("years_of_experience");

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
        });

        modelBuilder.Entity<StaffRole>(entity =>
        {
            entity.ToTable("staff_roles");

            entity.HasKey(e => e.Id).HasName("staff_roles_pkey");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.StaffId).HasColumnName("staff_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("students");

            entity.HasKey(e => e.Id).HasName("students_pkey");

            entity.HasIndex(e => new { e.BranchId, e.ClassId }, "idx_students_branch_class");
            entity.HasIndex(e => e.DivisionId, "idx_students_division");
            entity.HasIndex(e => e.RollNumber, "idx_students_roll_number");
            entity.HasIndex(e => new { e.BranchId, e.AdmissionNo }, "uq_students_admission_no")
                .IsUnique()
                .HasFilter("admission_no IS NOT NULL");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.AcademicYearId).HasColumnName("academic_year_id");
            entity.Property(e => e.AdmissionNo).HasColumnName("admission_no").HasMaxLength(50);
            entity.Property(e => e.AdmissionDate).HasColumnName("admission_date");
            entity.Property(e => e.AparId).HasColumnName("apar_id");
            entity.Property(e => e.PhotoUrl).HasColumnName("photo_url");
            entity.Property(e => e.SignUrl).HasColumnName("sign_url");
            entity.Property(e => e.FirstName).HasColumnName("first_name").HasMaxLength(100);
            entity.Property(e => e.MiddleName).HasColumnName("middle_name").HasMaxLength(100);
            entity.Property(e => e.LastName).HasColumnName("last_name").HasMaxLength(100);
            entity.Property(e => e.Gender).HasColumnName("gender").HasMaxLength(20);
            entity.Property(e => e.Dob).HasColumnName("dob");
            entity.Property(e => e.BirthPlace).HasColumnName("birth_place").HasMaxLength(200);
            entity.Property(e => e.BirthCertificateNo).HasColumnName("birth_certificate_no").HasMaxLength(100);
            entity.Property(e => e.BirthCertificateIssueDate).HasColumnName("birth_certificate_issue_date");
            entity.Property(e => e.ChildAadhaar).HasColumnName("child_aadhaar").HasMaxLength(20);
            entity.Property(e => e.Religion).HasColumnName("religion").HasMaxLength(50);
            entity.Property(e => e.Caste).HasColumnName("caste").HasMaxLength(50);
            entity.Property(e => e.Category).HasColumnName("category").HasMaxLength(50);
            entity.Property(e => e.Nationality).HasColumnName("nationality").HasMaxLength(50);
            entity.Property(e => e.BloodGroup).HasColumnName("blood_group").HasMaxLength(10);
            entity.Property(e => e.MarkIdentification).HasColumnName("mark_identification");
            entity.Property(e => e.HeightCm).HasColumnName("height_cm").HasPrecision(5, 2);
            entity.Property(e => e.WeightKg).HasColumnName("weight_kg").HasPrecision(5, 2);
            entity.Property(e => e.MedicalConditions).HasColumnName("medical_conditions");
            entity.Property(e => e.Allergies).HasColumnName("allergies");
            entity.Property(e => e.Disability).HasColumnName("disability");
            entity.Property(e => e.PreviousSchool).HasColumnName("previous_school").HasMaxLength(200);
            entity.Property(e => e.PreviousClass).HasColumnName("previous_class").HasMaxLength(50);
            entity.Property(e => e.PreviousExam).HasColumnName("previous_exam").HasMaxLength(100);
            entity.Property(e => e.PreviousExamResult).HasColumnName("previous_exam_result").HasMaxLength(50);
            entity.Property(e => e.PreviousPercentage).HasColumnName("previous_percentage").HasPrecision(5, 2);
            entity.Property(e => e.PreviousSchoolContactNumber).HasColumnName("previous_school_contact_number").HasMaxLength(200);
            entity.Property(e => e.PreviousExamBord).HasColumnName("previous_exam_bord").HasMaxLength(100);
            entity.Property(e => e.AddressLine1).HasColumnName("address_line1");
            entity.Property(e => e.AddressLine2).HasColumnName("address_line2");
            entity.Property(e => e.City).HasColumnName("city").HasMaxLength(100);
            entity.Property(e => e.State).HasColumnName("state").HasMaxLength(100);
            entity.Property(e => e.Pincode).HasColumnName("pincode").HasMaxLength(20);
            entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(20);
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(150);
            entity.Property(e => e.FatherName).HasColumnName("father_name").HasMaxLength(150);
            entity.Property(e => e.FatherPhone).HasColumnName("father_phone").HasMaxLength(20);
            entity.Property(e => e.FatherAadhaar).HasColumnName("father_aadhaar").HasMaxLength(20);
            entity.Property(e => e.FatherOccupation).HasColumnName("father_occupation").HasMaxLength(100);
            entity.Property(e => e.MotherName).HasColumnName("mother_name").HasMaxLength(150);
            entity.Property(e => e.MotherPhone).HasColumnName("mother_phone").HasMaxLength(20);
            entity.Property(e => e.MotherAadhaar).HasColumnName("mother_aadhaar").HasMaxLength(20);
            entity.Property(e => e.MotherOccupation).HasColumnName("mother_occupation").HasMaxLength(100);
            entity.Property(e => e.GuardianName).HasColumnName("guardian_name").HasMaxLength(150);
            entity.Property(e => e.GuardianPhone).HasColumnName("guardian_phone").HasMaxLength(20);
            entity.Property(e => e.FamilyIncome).HasColumnName("family_income").HasPrecision(12, 2);
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.DivisionId).HasColumnName("division_id");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.RollNumber).HasColumnName("roll_number").HasMaxLength(50);
            entity.Property(e => e.StudentStatus).HasColumnName("student_status").HasMaxLength(50).HasDefaultValue("ACTIVE");
            entity.Property(e => e.EnrollmentNo).HasColumnName("enrollment_no").HasMaxLength(100);
            entity.Property(e => e.IsAadhaarVerified).HasColumnName("is_aadhaar_verified").HasDefaultValue(false);
            entity.Property(e => e.PromotedToClassId).HasColumnName("promoted_to_class_id");
            entity.Property(e => e.LeavingDate).HasColumnName("leaving_date");
            entity.Property(e => e.IsTcIssued).HasColumnName("is_tc_issued").HasDefaultValue(false);
            entity.Property(e => e.LastExamName).HasColumnName("last_exam_name").HasMaxLength(200);
            entity.Property(e => e.LastExamResult).HasColumnName("last_exam_result").HasMaxLength(100);
            entity.Property(e => e.HasScholarship).HasColumnName("has_scholarship").HasDefaultValue(false);
            entity.Property(e => e.ScholarshipType).HasColumnName("scholarship_type").HasMaxLength(100);
            entity.Property(e => e.ScholarshipStatus).HasColumnName("scholarship_status").HasMaxLength(50);
            entity.Property(e => e.ScholarshipAmount).HasColumnName("scholarship_amount").HasPrecision(12, 2);
            entity.Property(e => e.ScholarshipEligible).HasColumnName("scholarship_eligible").HasDefaultValue(false);
            entity.Property(e => e.IsRteSeat).HasColumnName("is_rte_seat").HasDefaultValue(false);
            entity.Property(e => e.RteEligible).HasColumnName("rte_eligible").HasDefaultValue(false);
            entity.Property(e => e.RteApplicationNo).HasColumnName("rte_application_no").HasMaxLength(100);
            entity.Property(e => e.EwsDgCategory).HasColumnName("ews_dg_category").HasMaxLength(20);
            entity.Property(e => e.IsMinority).HasColumnName("is_minority").HasDefaultValue(false);
            entity.Property(e => e.IsBpl).HasColumnName("is_bpl").HasDefaultValue(false);
            entity.Property(e => e.BplCardNo).HasColumnName("bpl_card_no").HasMaxLength(100);
            entity.Property(e => e.UdiseCode).HasColumnName("udise_code").HasMaxLength(50);
            entity.Property(e => e.SingleGirlChild).HasColumnName("single_girl_child").HasDefaultValue(false);
            entity.Property(e => e.FirstGraduate).HasColumnName("first_graduate").HasDefaultValue(false);
        });

        modelBuilder.Entity<StudentResult>(entity =>
        {
            entity.ToTable("student_results");

            entity.HasKey(e => e.Id).HasName("student_results_pkey");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.ExamId).HasColumnName("exam_id");
            entity.Property(e => e.StudentId).HasColumnName("student_id");

            entity.Property(e => e.TotalMarks)
                .HasColumnName("total_marks")
                .HasPrecision(6, 2);

            entity.Property(e => e.Percentage)
                .HasColumnName("percentage")
                .HasPrecision(5, 2);

            entity.Property(e => e.Grade)
                .HasColumnName("grade")
                .HasMaxLength(5);

            entity.Property(e => e.Result)
                .HasColumnName("result")
                .HasMaxLength(20);

            entity.Property(e => e.Rank).HasColumnName("rank");

            entity.Property(e => e.GeneratedAt)
                .HasColumnName("generated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
        });

        modelBuilder.Entity<StudentSubject>(entity =>
        {
            entity.ToTable("student_subjects");

            entity.HasKey(e => e.Id).HasName("student_subjects_pkey");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.SubjectId).HasColumnName("subject_id");

            entity.Property(e => e.AcademicYear)
                .HasColumnName("academic_year")
                .HasMaxLength(20);
        });
        modelBuilder.Entity<Subject>(entity =>
        {
            entity.ToTable("subjects");

            entity.HasKey(e => e.Id).HasName("subjects_pkey");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(100);

            entity.Property(e => e.Code)
                .HasColumnName("code")
                .HasMaxLength(50);
        });

        modelBuilder.Entity<TeacherSubjectMap>(entity =>
        {
            entity.ToTable("teacher_subject_map");

            entity.HasKey(e => e.Id).HasName("teacher_subject_map_pkey");

            entity.HasIndex(e => new { e.StaffId, e.SubjectId, e.ClassId, e.SectionId, e.AcademicYear }, "unique_teacher_subject").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.StaffId).HasColumnName("staff_id");
            entity.Property(e => e.SubjectId).HasColumnName("subject_id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.DivisionId).HasColumnName("division_id");
            //entity.Property(e => e.SectionId).HasColumnName("section_id");  

            entity.Property(e => e.AcademicYear)
                .HasColumnName("academic_year")
                .HasMaxLength(20);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
        });

        modelBuilder.Entity<Timetable>(entity =>
        {
            entity.ToTable("timetable");

            entity.HasKey(e => e.Id).HasName("timetable_pkey");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            //entity.Property(e => e.SectionId).HasColumnName("section_id");
            entity.Property(e => e.SubjectId).HasColumnName("subject_id");
            entity.Property(e => e.TeacherId).HasColumnName("teacher_id");
            entity.Property(e => e.DayOfWeek).HasColumnName("day_of_week");
            entity.Property(e => e.PeriodNo).HasColumnName("period_no");
            entity.Property(e => e.AcademicYearId).HasColumnName("academic_year_id");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.HasIndex(e => e.Username, "users_username_key").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");

            entity.Property(e => e.Username)
                .HasColumnName("username")
                .HasMaxLength(100);

            entity.Property(e => e.Email)
                .HasColumnName("email")
                .HasMaxLength(150);

            entity.Property(e => e.Phone)
                .HasColumnName("phone")
                .HasMaxLength(20);

            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            entity.Property(e => e.LastLogin)
                .HasColumnName("last_login")
                .HasColumnType("timestamp without time zone");

            entity.Property(e => e.SecurityQuestion)
                .HasColumnName("security_question")
                .HasMaxLength(150);

            entity.Property(e => e.SecurityAnswer)
                .HasColumnName("security_answer")
                .HasMaxLength(50);

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
        });

        modelBuilder.Entity<TcCertificate>(entity =>
        {
            entity.ToTable("tc_certificates");

            entity.HasKey(e => e.Id).HasName("tc_certificates_pkey");

            entity.HasIndex(e => e.TcNumber, "ix_tc_certificates_tc_number").IsUnique();
            entity.HasIndex(e => new { e.AcademicYearId, e.ClassId }, "idx_tc_certificates_academic_year_class");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.AcademicYearId).HasColumnName("academic_year_id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.DivisionId).HasColumnName("division_id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");

            entity.Property(e => e.TcNumber)
                .HasColumnName("tc_number")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Sequence).HasColumnName("sequence");
            entity.Property(e => e.IssueDate).HasColumnName("issue_date");

            entity.Property(e => e.Reason)
                .HasColumnName("reason")
                .HasMaxLength(200);

            entity.Property(e => e.Remarks).HasColumnName("remarks");

            entity.Property(e => e.IsDuplicate)
                .HasColumnName("is_duplicate")
                .HasDefaultValue(false);

            entity.Property(e => e.IssuedByStaffId).HasColumnName("issued_by_staff_id");

            entity.Property(e => e.Conduct)
                .HasColumnName("conduct")
                .HasMaxLength(100);

            entity.Property(e => e.FeesCleared)
                .HasColumnName("fees_cleared")
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
        });

        modelBuilder.Entity<BonafideCertificate>(entity =>
        {
            entity.ToTable("bonafide_certificates");

            entity.HasKey(e => e.Id).HasName("bonafide_certificates_pkey");

            entity.HasIndex(e => e.BonafideNumber, "ix_bonafide_certificates_bonafide_number").IsUnique();
            entity.HasIndex(e => new { e.AcademicYearId, e.ClassId }, "idx_bonafide_certificates_academic_year_class");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.AcademicYearId).HasColumnName("academic_year_id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.DivisionId).HasColumnName("division_id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");

            entity.Property(e => e.BonafideNumber)
                .HasColumnName("bonafide_number")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Sequence).HasColumnName("sequence");
            entity.Property(e => e.IssueDate).HasColumnName("issue_date");

            entity.Property(e => e.Purpose)
                .HasColumnName("purpose")
                .HasMaxLength(200);

            entity.Property(e => e.Remarks).HasColumnName("remarks");
            entity.Property(e => e.ValidFrom).HasColumnName("valid_from");
            entity.Property(e => e.ValidTo).HasColumnName("valid_to");

            entity.Property(e => e.IsDuplicate)
                .HasColumnName("is_duplicate")
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
        });

        modelBuilder.Entity<HallTicket>(entity =>
        {
            entity.ToTable("hall_tickets");

            entity.HasKey(e => e.Id).HasName("hall_tickets_pkey");

            entity.HasIndex(e => new { e.StudentId, e.ExamId }, "ix_hall_tickets_student_id_exam_id").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.ExamId).HasColumnName("exam_id");
            entity.Property(e => e.AcademicYearId).HasColumnName("academic_year_id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.DivisionId).HasColumnName("division_id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");

            entity.Property(e => e.TicketNumber)
                .HasColumnName("ticket_number")
                .HasMaxLength(100);

            entity.Property(e => e.SeatNumber)
                .HasColumnName("seat_number")
                .HasMaxLength(50);

            entity.Property(e => e.ExamCentre)
                .HasColumnName("exam_centre")
                .HasMaxLength(200);

            entity.Property(e => e.GeneratedAt)
                .HasColumnName("generated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
        });
        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.ToTable("leave_requests");

            entity.HasKey(e => e.Id).HasName("leave_requests_pkey");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.FromDate).HasColumnName("from_date");
            entity.Property(e => e.ToDate).HasColumnName("to_date");

            entity.Property(e => e.LeaveType)
                .HasColumnName("leave_type")
                .HasMaxLength(100);

            entity.Property(e => e.Reason).HasColumnName("reason");

            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.RequestedAt)
                .HasColumnName("requested_at")
                .HasColumnType("timestamp without time zone");

            entity.Property(e => e.RequestedBy).HasColumnName("requested_by");

            entity.Property(e => e.ApprovedAt)
                .HasColumnName("approved_at")
                .HasColumnType("timestamp without time zone");

            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");

            entity.Property(e => e.RejectionReason).HasColumnName("rejection_reason");
        });

        modelBuilder.Entity<AttendanceCorrectionRequest>(entity =>
        {
            entity.ToTable("attendance_correction_requests");

            entity.HasKey(e => e.Id).HasName("attendance_correction_requests_pkey");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.AttendanceId).HasColumnName("attendance_id");

            entity.Property(e => e.Reason)
                .HasColumnName("reason")
                .HasMaxLength(2000)
                .IsRequired();

            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.RequestedAt)
                .HasColumnName("requested_at")
                .HasColumnType("timestamp without time zone");

            entity.Property(e => e.RequestedBy).HasColumnName("requested_by");

            entity.Property(e => e.ApprovedAt)
                .HasColumnName("approved_at")
                .HasColumnType("timestamp without time zone");

            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");

            entity.Property(e => e.RejectionReason).HasColumnName("rejection_reason");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
