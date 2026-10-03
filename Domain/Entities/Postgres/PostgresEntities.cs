//// This file contains entity classes for all tables in the new Postgres schema.
//// Each class is a POCO representing a table, with properties matching the columns.
//// Add navigation properties and data annotations as needed for EF Core.

//namespace Domain.Entities.Postgres
//{
//    public class SchoolGroup
//    {
//        public Guid Id { get; set; }
//        public string? Name { get; set; }
//        public string? Code { get; set; }
//        public string? Address { get; set; }
//        public string? Phone { get; set; }
//        public string? Email { get; set; }
//        public DateTime CreatedAt { get; set; }
//    }

//    public class InstitutionType
//    {
//        public Guid Id { get; set; }
//        public string? Name { get; set; }
//    }

//    public class Medium
//    {
//        public Guid Id { get; set; }
//        public string? Name { get; set; }
//    }

//    public class AcademicYear
//    {
//        public Guid Id { get; set; }
//        public string? YearName { get; set; }
//        public DateTime? StartDate { get; set; }
//        public DateTime? EndDate { get; set; }
//        public bool IsActive { get; set; }
//    }

//    public class Branch
//    {
//        public Guid Id { get; set; }
//        public Guid? GroupId { get; set; }
//        public Guid? InstitutionTypeId { get; set; }
//        public Guid? MediumId { get; set; }
//        public string? SchoolUID { get; set; }
//        public string? Name { get; set; }
//        public string? Address { get; set; }
//        public string? City { get; set; }
//        public string? State { get; set; }
//        public string? Phone { get; set; }
//        public string? Email { get; set; }
//        public DateTime CreatedAt { get; set; }
//    }

//    public class Role
//    {
//        public Guid Id { get; set; }
//        public string? Name { get; set; }
//        public string? Description { get; set; }
//    }

//    public class Class
//    {
//        public Guid Id { get; set; }
//        public Guid? BranchId { get; set; }
//        public string? Name { get; set; }
//    }

//    public class Division
//    {
//        public Guid Id { get; set; }
//        public Guid? ClassId { get; set; }
//        public Guid? BranchId { get; set; }
//        public string? Name { get; set; }
//        public string? Code { get; set; }
//        public int? AvailableSeats { get; set; }
//        public DateTime CreatedAt { get; set; }
//    }

//    public class Section
//    {
//        public Guid Id { get; set; }
//        public Guid? ClassId { get; set; }
//        public Guid? DivisionId { get; set; }
//        public string? Name { get; set; }
//    }

//    public class Department
//    {
//        public Guid Id { get; set; }
//        public string? Name { get; set; }
//    }

//    public class Staff
//    {
//        public Guid Id { get; set; }
//        public Guid? BranchId { get; set; }
//        public Guid? DeptId { get; set; }
//        public string? StaffCode { get; set; }
//        public string? Designation { get; set; }
//        public string? FirstName { get; set; }
//        public string? MiddleName { get; set; }
//        public string? LastName { get; set; }
//        public string? Gender { get; set; }
//        public DateTime? Dob { get; set; }
//        public string? Phone { get; set; }
//        public string? Email { get; set; }
//        public string? Address { get; set; }
//        public string? City { get; set; }
//        public string? State { get; set; }
//        public DateTime? JoiningDate { get; set; }
//        public decimal? Salary { get; set; }
//        public string? Qualification { get; set; }
//        public string? QualificationDetails { get; set; }
//        public string? PreviousEmployer { get; set; }
//        public int? YearsOfExperience { get; set; }
//        public bool IsActive { get; set; }
//        public DateTime CreatedAt { get; set; }
//    }

//    public class StaffRole
//    {
//        public Guid Id { get; set; }
//        public Guid? StaffId { get; set; }
//        public Guid? RoleId { get; set; }
//    }

//    public class User
//    {
//        public Guid Id { get; set; }
//        public Guid? BranchId { get; set; }
//        public Guid? RoleId { get; set; }
//        public Guid? StaffId { get; set; }
//        public string? Username { get; set; }
//        public string? Email { get; set; }
//        public string? Phone { get; set; }
//        public string? PasswordHash { get; set; }
//        public bool IsActive { get; set; }
//        public DateTime? LastLogin { get; set; }
//        public string? SecurityQuestion { get; set; }
//        public string? SecurityAnswer { get; set; }
//        public DateTime UpdatedAt { get; set; }
//        public DateTime CreatedAt { get; set; }
//    }

//    // ...continue for all other tables as POCOs...
//}
