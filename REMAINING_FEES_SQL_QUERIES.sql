-- =============================================================================
-- REMAINING FEES CALCULATION QUERIES
-- =============================================================================

-- 1. GET REMAINING FEES FOR A SPECIFIC STUDENT
-- Returns: Student details with total due fees, paid fees, and remaining fees
SELECT 
    s."Id" as StudentId,
    s."FirstName" || ' ' || COALESCE(s."LastName", '') as StudentName,
    s."AdmissionNo",
    s."ClassId",
    s."DivisionId",
    s."BranchId",
    COALESCE(SUM(cfm."Amount"), 0) as TotalDueFees,
    COALESCE(SUM(p."Amount"), 0) as PaidAmount,
    COALESCE(SUM(cfm."Amount"), 0) - COALESCE(SUM(p."Amount"), 0) as RemainingFees
FROM 
    public."Students" s
LEFT JOIN 
    public."ClassFeeMap" cfm ON s."ClassId" = cfm."ClassId" 
        AND cfm."AcademicYearId" = s."AcademicYearId"
LEFT JOIN 
    public."Payments" p ON s."Id" = p."StudentId"
WHERE 
    s."Id" = '55836f20-2f9d-4755-b64c-c755741f05a9'
GROUP BY 
    s."Id", s."FirstName", s."LastName", s."AdmissionNo", 
    s."ClassId", s."DivisionId", s."BranchId";


-- 2. GET CLASS-WISE REMAINING FEES
-- Returns: Class details with total due fees, paid fees, and remaining fees
SELECT 
    c."Id" as ClassId,
    c."Name" as ClassName,
    c."BranchId",
    COUNT(DISTINCT s."Id") as TotalStudents,
    COALESCE(SUM(cfm."Amount"), 0) as TotalClassDueFees,
    COALESCE(SUM(p."Amount"), 0) as TotalPaidAmount,
    COALESCE(SUM(cfm."Amount"), 0) - COALESCE(SUM(p."Amount"), 0) as TotalRemainingFees,
    ROUND(
        COALESCE(SUM(p."Amount"), 0)::NUMERIC / 
        NULLIF(COALESCE(SUM(cfm."Amount"), 0), 0)::NUMERIC * 100, 2
    ) as PercentagePaid
FROM 
    public."Class" c
LEFT JOIN 
    public."Students" s ON c."Id" = s."ClassId"
LEFT JOIN 
    public."ClassFeeMap" cfm ON c."Id" = cfm."ClassId"
LEFT JOIN 
    public."Payments" p ON s."Id" = p."StudentId"
WHERE 
    c."Id" = '55836f20-2f9d-4755-b64c-c755741f05a9'
GROUP BY 
    c."Id", c."Name", c."BranchId";


-- 3. GET DIVISION-WISE REMAINING FEES
-- Returns: Division details with total due fees, paid fees, and remaining fees
SELECT 
    d."Id" as DivisionId,
    d."Name" as DivisionName,
    d."Code" as DivisionCode,
    c."Name" as ClassName,
    d."BranchId",
    COUNT(DISTINCT s."Id") as TotalStudents,
    COALESCE(SUM(cfm."Amount"), 0) as TotalDivisionDueFees,
    COALESCE(SUM(p."Amount"), 0) as TotalPaidAmount,
    COALESCE(SUM(cfm."Amount"), 0) - COALESCE(SUM(p."Amount"), 0) as TotalRemainingFees,
    ROUND(
        COALESCE(SUM(p."Amount"), 0)::NUMERIC / 
        NULLIF(COALESCE(SUM(cfm."Amount"), 0), 0)::NUMERIC * 100, 2
    ) as PercentagePaid
FROM 
    public."Division" d
LEFT JOIN 
    public."Class" c ON d."ClassId" = c."Id"
LEFT JOIN 
    public."Students" s ON d."Id" = s."DivisionId"
LEFT JOIN 
    public."ClassFeeMap" cfm ON d."ClassId" = cfm."ClassId"
LEFT JOIN 
    public."Payments" p ON s."Id" = p."StudentId"
WHERE 
    d."Id" = 'e24888e0-2f03-4de0-b6c1-19c90b449d51'
GROUP BY 
    d."Id", d."Name", d."Code", c."Name", d."BranchId";


-- 4. GET BRANCH-WISE REMAINING FEES
-- Returns: Branch details with total due fees, paid fees, and remaining fees
SELECT 
    b."Id" as BranchId,
    b."Name" as BranchName,
    b."Address",
    b."City",
    b."Phone",
    COUNT(DISTINCT s."Id") as TotalStudents,
    COUNT(DISTINCT c."Id") as TotalClasses,
    COALESCE(SUM(cfm."Amount"), 0) as TotalBranchDueFees,
    COALESCE(SUM(p."Amount"), 0) as TotalPaidAmount,
    COALESCE(SUM(cfm."Amount"), 0) - COALESCE(SUM(p."Amount"), 0) as TotalRemainingFees,
    ROUND(
        COALESCE(SUM(p."Amount"), 0)::NUMERIC / 
        NULLIF(COALESCE(SUM(cfm."Amount"), 0), 0)::NUMERIC * 100, 2
    ) as PercentagePaid
FROM 
    public."Branch" b
LEFT JOIN 
    public."Class" c ON b."Id" = c."BranchId"
LEFT JOIN 
    public."Students" s ON b."Id" = s."BranchId"
LEFT JOIN 
    public."ClassFeeMap" cfm ON c."Id" = cfm."ClassId"
LEFT JOIN 
    public."Payments" p ON s."Id" = p."StudentId"
WHERE 
    b."Id" IS NOT NULL
GROUP BY 
    b."Id", b."Name", b."Address", b."City", b."Phone"
ORDER BY 
    TotalRemainingFees DESC;


-- 5. GET STUDENT-WISE REMAINING FEES BY FEE HEAD (CLASS-WISE)
-- Returns: Detailed breakdown of remaining fees by fee head for a student
SELECT 
    s."Id" as StudentId,
    s."FirstName" || ' ' || COALESCE(s."LastName", '') as StudentName,
    fh."Id" as FeeHeadId,
    fh."Name" as FeeName,
    cfm."InstallmentNo" as Installment,
    COALESCE(cfm."Amount", 0) as DueFees,
    COALESCE(SUM(pfl."Amount"), 0) as PaidAmount,
    COALESCE(cfm."Amount", 0) - COALESCE(SUM(pfl."Amount"), 0) as RemainingFees
FROM 
    public."Students" s
LEFT JOIN 
    public."ClassFeeMap" cfm ON s."ClassId" = cfm."ClassId" 
        AND cfm."AcademicYearId" = s."AcademicYearId"
LEFT JOIN 
    public."FeeHead" fh ON cfm."FeeHeadId" = fh."Id"
LEFT JOIN 
    public."Payments" p ON s."Id" = p."StudentId"
LEFT JOIN 
    public."PaymentFeeLine" pfl ON p."Id" = pfl."PaymentId" 
        AND fh."Id" = pfl."FeeHeadId"
WHERE 
    s."Id" = '55836f20-2f9d-4755-b64c-c755741f05a9'
GROUP BY 
    s."Id", s."FirstName", s."LastName", fh."Id", fh."Name", 
    cfm."InstallmentNo", cfm."Amount"
ORDER BY 
    COALESCE(cfm."InstallmentNo", 0) ASC;


-- 6. GET ALL STUDENTS WITH PENDING FEES (DIVISION-WISE)
-- Returns: List of all students in a division with pending fees
SELECT 
    s."Id" as StudentId,
    s."FirstName" || ' ' || COALESCE(s."LastName", '') as StudentName,
    s."AdmissionNo",
    d."Name" as DivisionName,
    c."Name" as ClassName,
    s."BranchId",
    COALESCE(SUM(cfm."Amount"), 0) as TotalDueFees,
    COALESCE(SUM(p."Amount"), 0) as PaidAmount,
    COALESCE(SUM(cfm."Amount"), 0) - COALESCE(SUM(p."Amount"), 0) as RemainingFees
FROM 
    public."Students" s
LEFT JOIN 
    public."Division" d ON s."DivisionId" = d."Id"
LEFT JOIN 
    public."Class" c ON s."ClassId" = c."Id"
LEFT JOIN 
    public."ClassFeeMap" cfm ON s."ClassId" = cfm."ClassId"
LEFT JOIN 
    public."Payments" p ON s."Id" = p."StudentId"
WHERE 
    s."DivisionId" = 'e24888e0-2f03-4de0-b6c1-19c90b449d51'
    AND s."IsActive" = true
GROUP BY 
    s."Id", s."FirstName", s."LastName", s."AdmissionNo", 
    d."Name", c."Name", s."BranchId"
HAVING 
    COALESCE(SUM(cfm."Amount"), 0) - COALESCE(SUM(p."Amount"), 0) > 0
ORDER BY 
    RemainingFees DESC;


-- 7. GET PAYMENT SUMMARY (BRANCH-WISE & MONTH-WISE)
-- Returns: Monthly payment collection by branch
SELECT 
    DATE_TRUNC('month', p."PaymentDate")::DATE as PaymentMonth,
    b."Name" as BranchName,
    COUNT(DISTINCT p."StudentId") as StudentsWithPayments,
    COUNT(DISTINCT p."Id") as TotalPayments,
    COALESCE(SUM(p."Amount"), 0) as TotalCollected
FROM 
    public."Payments" p
LEFT JOIN 
    public."Branch" b ON p."BranchId" = b."Id"
WHERE 
    p."PaymentDate" >= CURRENT_DATE - INTERVAL '1 year'
GROUP BY 
    DATE_TRUNC('month', p."PaymentDate")::DATE,
    b."Name"
ORDER BY 
    PaymentMonth DESC, BranchName ASC;
