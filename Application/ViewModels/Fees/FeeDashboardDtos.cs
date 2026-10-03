using System;
using System.Collections.Generic;

namespace Application.ViewModels.Fees;

public class StudentFeeDashboardDto
{
    public Guid StudentId { get; set; }
    public string? RollNumber { get; set; }
    public decimal TotalFees { get; set; }
    public decimal PaidFees { get; set; }
    public decimal RemainingFees { get; set; }

    public List<StudentFeeHeadDto> FeeHeads { get; set; } = new();
    public List<StudentInstallmentDto> Installments { get; set; } = new();
}

public class StudentFeeHeadDto
{
    public Guid FeeHeadId { get; set; }
    public string FeeHeadName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
}

public class StudentInstallmentDto
{
    public Guid InstallmentId { get; set; }
    public string InstallmentName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTime? DueDate { get; set; }
}

public class ClassFeeDashboardDto
{
    public Guid ClassId { get; set; }
    public decimal TotalFees { get; set; }
    public decimal CollectedFees { get; set; }
    public decimal PendingFees { get; set; }
    public int TotalStudents { get; set; }
    public decimal CollectionRate { get; set; }
}

public class DivisionFeeDashboardDto
{
    public Guid DivisionId { get; set; }
    public decimal TotalFees { get; set; }
    public decimal CollectedFees { get; set; }
    public decimal PendingFees { get; set; }
    public int TotalStudents { get; set; }
    public decimal CollectionRate { get; set; }
}

public class BranchFeeDashboardDto
{
    public Guid BranchId { get; set; }
    public decimal TotalFees { get; set; }
    public decimal CollectedFees { get; set; }
    public decimal PendingFees { get; set; }
    public int TotalStudents { get; set; }
    public decimal CollectionRate { get; set; }
}

public class FeeCollectionTrendDto
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
}

public class FeeHeadAnalyticsDto
{
    public Guid FeeHeadId { get; set; }
    public string FeeHeadName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}

public class InstallmentCollectionDto
{
    public Guid InstallmentId { get; set; }
    public string InstallmentName { get; set; } = string.Empty;
    public decimal ExpectedAmount { get; set; }
    public decimal CollectedAmount { get; set; }
    /// <summary>Expected minus collected for this installment row.</summary>
    public decimal PendingAmount { get; set; }
    /// <summary>Collected / expected * 100 when expected &gt; 0.</summary>
    public decimal CollectionPercent { get; set; }
    public string? BranchName { get; set; }
    public string? ClassName { get; set; }
    public string? DivisionName { get; set; }
}

public class StudentOutstandingFeeDto
{
    public Guid StudentId { get; set; }
    public string? RollNumber { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? DivisionId { get; set; }
    public decimal RemainingAmount { get; set; }
}

public class ClassOutstandingFeeDto
{
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public decimal RemainingAmount { get; set; }
}

public class DivisionOutstandingFeeDto
{
    public Guid DivisionId { get; set; }
    public string DivisionName { get; set; } = string.Empty;
    public decimal RemainingAmount { get; set; }
}

public class BranchOutstandingFeeDto
{
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public decimal RemainingAmount { get; set; }
}

public class StudentFeeHeadStatusDto
{
    public Guid StudentId { get; set; }
    public string? RollNumber { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? DivisionId { get; set; }
    public Guid FeeHeadId { get; set; }
    public string FeeHeadName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
}

