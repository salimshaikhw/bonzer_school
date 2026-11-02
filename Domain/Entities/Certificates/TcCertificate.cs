using Domain.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Certificates;

[Table("tc_certificates")]
[Index(nameof(TcNumber), IsUnique = true)]
[Index(nameof(AcademicYearId), nameof(ClassId))]
[Index(nameof(Status))]
public partial class TcCertificate : AuditableEntity
{
    [References(typeof(Student))]
    public Guid StudentId { get; set; }

    [References(typeof(AcademicYear))]
    public Guid? AcademicYearId { get; set; }

    [References(typeof(Class))]
    public Guid? ClassId { get; set; }

    [References(typeof(Division))]
    public Guid? DivisionId { get; set; }

    [References(typeof(Branch))]
    public Guid? BranchId { get; set; }

    [MaxLength(100)]
    [Required]
    public string TcNumber { get; set; } = null!;

    public int Sequence { get; set; }

    public DateOnly IssueDate { get; set; }

    [MaxLength(200)]
    public string? Reason { get; set; }

    public string? Remarks { get; set; }

    [DefaultValue(false)]
    public bool IsDuplicate { get; set; }

    [References(typeof(Staff))]
    public Guid? IssuedByStaffId { get; set; }

    [MaxLength(100)]
    public string? Conduct { get; set; }

    [DefaultValue(true)]
    public bool? FeesCleared { get; set; }

    public DateTime? DeletedAt { get; set; }

    // Snapshot of the student at the time the TC was raised.
    public DateOnly? AdmissionDateSnapshot { get; set; }
    public DateOnly? LeavingDate { get; set; }
    [MaxLength(100)]
    public string? LastClassAttended { get; set; }
    [MaxLength(200)]
    public string? LastExamNameSnapshot { get; set; }
    [MaxLength(50)]
    public string? LastExamGradeSnapshot { get; set; }

    // Approval workflow (driven by the tc_* database functions, see TcWorkflowFunctions migration).
    /// <summary>DRAFT / PENDING_DEPARTMENT_CLEARANCE / PENDING_HEAD_CLERK / PENDING_CLASS_TEACHER / PENDING_PRINCIPAL / APPROVED / REJECTED / ISSUED</summary>
    [MaxLength(50)]
    [DefaultValue("DRAFT")]
    [Required]
    public string Status { get; set; } = "DRAFT";
    public Guid? ClassTeacherStaffId { get; set; }
    public Guid? HeadClerkApprovedBy { get; set; }
    public DateTime? HeadClerkApprovedAt { get; set; }
    public Guid? ClassTeacherApprovedBy { get; set; }
    public DateTime? ClassTeacherApprovedAt { get; set; }
    public Guid? PrincipalApprovedBy { get; set; }
    public DateTime? PrincipalApprovedAt { get; set; }
    public DateTime? IssuedAt { get; set; }
    public Guid? RejectedBy { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }
}

