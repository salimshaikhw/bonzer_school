using System;
using System.Collections.Generic;

namespace Infrastructure;

public partial class Staff
{
    public Guid Id { get; set; }

    public Guid? BranchId { get; set; }

    public Guid? DeptId { get; set; }

    public string? StaffCode { get; set; }

    public string? Designation { get; set; }

    public string? FirstName { get; set; }

    public string? MiddleName { get; set; }

    public string? LastName { get; set; }

    public string? Gender { get; set; }

    public DateOnly? Dob { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public DateOnly? JoiningDate { get; set; }

    public decimal? Salary { get; set; }

    public string? Qualification { get; set; }

    public string? Qualificationdetails { get; set; }

    public string? PreviousEmployer { get; set; }

    public int? YearsOfExperience { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }
}
