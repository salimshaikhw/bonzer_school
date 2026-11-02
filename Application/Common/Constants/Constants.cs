namespace Application.Common.Constants
{
    public static class Constants
    {
        public const string DateDisplayFormat = "dd-MM-yyyy";
    }

    /// <summary>
    /// ASP.NET Identity role names (what goes into the JWT). Only role names may be declared as
    /// public constants here: UserService validates add/remove-role requests against them.
    /// </summary>
    public static class Roles
    {
        public const string GroupAdmin = "GroupAdmin";
        public const string BranchAdmin = "BranchAdmin";
        public const string Principal = "Principal";
        public const string VicePrincipal = "VicePrincipal";
        public const string Accountant = "Accountant";
        public const string Hod = "HOD";
        public const string Teacher = "Teacher";
        public const string ClassTeacher = "ClassTeacher";
        public const string SportsTeacher = "SportsTeacher";
        public const string Librarian = "Librarian";
        public const string Parent = "Parent";
        public const string Student = "Student";

        /// <summary>Every role, in seeding order.</summary>
        public static readonly IReadOnlyList<string> All = new[]
        {
            GroupAdmin, BranchAdmin, Principal, VicePrincipal, Accountant, Hod,
            Teacher, ClassTeacher, SportsTeacher, Librarian, Parent, Student,
        };

        /// <summary>Human-readable names for screens and the legacy roles table.</summary>
        public static readonly IReadOnlyDictionary<string, string> DisplayNames = new Dictionary<string, string>
        {
            [GroupAdmin] = "Group Admin",
            [BranchAdmin] = "Branch Admin",
            [Principal] = "Principal",
            [VicePrincipal] = "Vice Principal",
            [Accountant] = "Accountant",
            [Hod] = "Head of Department",
            [Teacher] = "Teacher",
            [ClassTeacher] = "Class Teacher",
            [SportsTeacher] = "Sports Teacher",
            [Librarian] = "Librarian",
            [Parent] = "Parent",
            [Student] = "Student",
        };

        /// <summary>What each role is for.</summary>
        public static readonly IReadOnlyDictionary<string, string> Descriptions = new Dictionary<string, string>
        {
            [GroupAdmin] = "Runs the school group (trust): all branches, branch setup, user roles.",
            [BranchAdmin] = "Runs one branch (school office): users, academic year, TC head-clerk step.",
            [Principal] = "Heads the school: approves and issues certificates, manages staff and academics.",
            [VicePrincipal] = "Deputy head: same school-level rights as the principal.",
            [Accountant] = "Fees: fee heads, structures, invoices, payments, dues.",
            [Hod] = "Head of department: subjects, timetable, exam setup.",
            [Teacher] = "Teaches subjects: attendance, homework, marks.",
            [ClassTeacher] = "Owns a class/division: students, leave approval, TC class-teacher step.",
            [SportsTeacher] = "Sports teacher: attendance, homework, marks for sports activities.",
            [Librarian] = "Library: no-dues clearance for TC, read access to students.",
            [Parent] = "Parent portal: own children's fees, attendance, leave requests.",
            [Student] = "Student portal: own attendance, results, leave requests.",
        };
    }

    /// <summary>
    /// Platform (SaaS operator) roles. Kept out of <see cref="Roles"/> on purpose: UserService only lets
    /// school admins grant the constants declared there, so these can never be granted through the API.
    /// A platform user has no tenant; the Super Admin is seeded from PlatformAdmin:Email/Password.
    /// </summary>
    public static class PlatformRoles
    {
        /// <summary>Creates and manages tenants (school groups) and the shared lookup tables.</summary>
        public const string SuperAdmin = "SuperAdmin";

        public static readonly IReadOnlyList<string> All = new[] { SuperAdmin };
    }

    /// <summary>
    /// Comma-separated role lists for <c>[Authorize(Roles = RoleGroups.X)]</c>.
    /// Read endpoints only require login; these groups guard the endpoints that change data.
    /// </summary>
    public static class RoleGroups
    {
        /// <summary>Platform Super Admin only: tenants and the shared catalog lookups (institution types, mediums).</summary>
        public const string PlatformAdmin = PlatformRoles.SuperAdmin;

        /// <summary>Group Admin only: school group, branches.</summary>
        public const string GroupAdminOnly = Roles.GroupAdmin;

        /// <summary>Group Admin, Branch Admin.</summary>
        public const string Administrators = Roles.GroupAdmin + "," + Roles.BranchAdmin;

        /// <summary>Administrators + Principal, Vice Principal.</summary>
        public const string SchoolLeadership = Administrators + "," + Roles.Principal + "," + Roles.VicePrincipal;

        /// <summary>School leadership + HOD: subjects, classes, timetable, exam setup.</summary>
        public const string AcademicLeadership = SchoolLeadership + "," + Roles.Hod;

        /// <summary>Academic leadership + all teachers: attendance, homework, marks.</summary>
        public const string TeachingStaff = AcademicLeadership + "," + Roles.Teacher + "," + Roles.ClassTeacher + "," + Roles.SportsTeacher;

        /// <summary>School leadership + Accountant: fees and payments.</summary>
        public const string Finance = SchoolLeadership + "," + Roles.Accountant;

        /// <summary>School leadership + Class Teacher: admissions and student records.</summary>
        public const string StudentManagers = SchoolLeadership + "," + Roles.ClassTeacher;

        /// <summary>Every staff role.</summary>
        public const string AllStaff = TeachingStaff + "," + Roles.Accountant + "," + Roles.Librarian;

        /// <summary>Every role, including Parent and Student.</summary>
        public const string Everyone = AllStaff + "," + Roles.Parent + "," + Roles.Student;

        // TC approval chain (see TcWorkflowFunctions migration). The branch office acts as head clerk.
        public const string TcRequesters = StudentManagers + "," + Roles.Parent;
        public const string TcHeadClerk = Administrators;
        public const string TcClassTeacher = Roles.ClassTeacher;
        public const string TcPrincipal = Roles.Principal + "," + Roles.VicePrincipal;
        public const string TcApprovers = TcHeadClerk + "," + Roles.ClassTeacher + "," + TcPrincipal;
    }
}
