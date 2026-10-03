-- =============================================================================
-- STUDENT REMAINING FEES BY PAYMENT HEAD (FEE BREAKDOWN)
-- Using PaymentFeeLine and ClassFeeMap tables
-- =============================================================================

-- 1. DETAILED: Student Remaining Fees by Fee Head (Payment Head)
-- Returns: For each student, each fee head, shows due, paid, and remaining
SELECT 
    s."Id" as StudentId,
    s."FirstName" || ' ' || COALESCE(s."LastName", '') as StudentName,
    s."AdmissionNo",
    s."ClassId",
    s."DivisionId",
    s."BranchId",
    fh."Id" as FeeHeadId,
    fh."Name" as FeeHeadName,
    fh."AccountCode",
    cfm."InstallmentNo" as Installment,
    COALESCE(cfm."Amount", 0) as DueFeeAmount,
    COALESCE(SUM(pfl."Amount"), 0) as PaidAmount,
    COALESCE(cfm."Amount", 0) - COALESCE(SUM(pfl."Amount"), 0) as RemainingFees,
    COUNT(DISTINCT p."Id") as PaymentCount,
    MAX(p."PaymentDate") as LastPaymentDate,
    p."PaymentMethod" as LastPaymentMethod
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
    s."Id", s."FirstName", s."LastName", s."AdmissionNo",
    s."ClassId", s."DivisionId", s."BranchId",
    fh."Id", fh."Name", fh."AccountCode",
    cfm."InstallmentNo", cfm."Amount", p."PaymentMethod"
ORDER BY 
    COALESCE(cfm."InstallmentNo", 0) ASC,
    fh."Name" ASC;


-- 2. SUMMARY: Student Total Remaining Fees by Fee Head
-- Returns: Summary of remaining fees for student, grouped by fee head
SELECT 
    s."Id" as StudentId,
    s."FirstName" || ' ' || COALESCE(s."LastName", '') as StudentName,
    fh."Id" as FeeHeadId,
    fh."Name" as FeeHeadName,
    COALESCE(SUM(cfm."Amount"), 0) as TotalDueFees,
    COALESCE(SUM(pfl."Amount"), 0) as TotalPaidAmount,
    COALESCE(SUM(cfm."Amount"), 0) - COALESCE(SUM(pfl."Amount"), 0) as RemainingFees,
    CASE 
        WHEN COALESCE(SUM(cfm."Amount"), 0) = 0 THEN 0
        ELSE ROUND(
            COALESCE(SUM(pfl."Amount"), 0)::NUMERIC / 
            COALESCE(SUM(cfm."Amount"), 0)::NUMERIC * 100, 2
        )
    END as PercentagePaid,
    COUNT(DISTINCT p."Id") as TotalPayments
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
    s."Id", s."FirstName", s."LastName", fh."Id", fh."Name"
ORDER BY 
    RemainingFees DESC;


-- 3. PAYMENT HISTORY: Student Payment Details by Fee Head
-- Returns: Complete payment history for student, showing which fee heads were paid
SELECT 
    s."Id" as StudentId,
    s."FirstName" || ' ' || COALESCE(s."LastName", '') as StudentName,
    p."Id" as PaymentId,
    p."InvoiceId",
    p."PaymentDate",
    p."PaymentMethod",
    p."BankName",
    fh."Id" as FeeHeadId,
    fh."Name" as FeeHeadName,
    pfl."Amount" as PaidAmount,
    p."Remarks"
FROM 
    public."Students" s
INNER JOIN 
    public."Payments" p ON s."Id" = p."StudentId"
LEFT JOIN 
    public."PaymentFeeLine" pfl ON p."Id" = pfl."PaymentId"
LEFT JOIN 
    public."FeeHead" fh ON pfl."FeeHeadId" = fh."Id"
WHERE 
    s."Id" = '55836f20-2f9d-4755-b64c-c755741f05a9'
ORDER BY 
    p."PaymentDate" DESC, fh."Name" ASC;


-- 4. CLASS WISE PAYMENT HEAD ANALYSIS
-- Returns: For all students in a class, show payment head-wise remaining fees
SELECT 
    c."Id" as ClassId,
    c."Name" as ClassName,
    fh."Id" as FeeHeadId,
    fh."Name" as FeeHeadName,
    COUNT(DISTINCT s."Id") as TotalStudents,
    COALESCE(SUM(cfm."Amount"), 0) as TotalDueAmount,
    COALESCE(SUM(pfl."Amount"), 0) as TotalPaidAmount,
    COALESCE(SUM(cfm."Amount"), 0) - COALESCE(SUM(pfl."Amount"), 0) as TotalRemainingFees,
    ROUND(
        COALESCE(SUM(pfl."Amount"), 0)::NUMERIC / 
        NULLIF(COALESCE(SUM(cfm."Amount"), 0), 0)::NUMERIC * 100, 2
    ) as PercentagePaid
FROM 
    public."Class" c
LEFT JOIN 
    public."Students" s ON c."Id" = s."ClassId"
LEFT JOIN 
    public."ClassFeeMap" cfm ON c."Id" = cfm."ClassId"
LEFT JOIN 
    public."FeeHead" fh ON cfm."FeeHeadId" = fh."Id"
LEFT JOIN 
    public."Payments" p ON s."Id" = p."StudentId"
LEFT JOIN 
    public."PaymentFeeLine" pfl ON p."Id" = pfl."PaymentId" 
        AND fh."Id" = pfl."FeeHeadId"
WHERE 
    c."Id" = '55836f20-2f9d-4755-b64c-c755741f05a9'
GROUP BY 
    c."Id", c."Name", fh."Id", fh."Name"
ORDER BY 
    fh."Name" ASC;


-- 5. DIVISION WISE PAYMENT HEAD ANALYSIS
-- Returns: For all students in a division, show payment head-wise remaining fees
SELECT 
    d."Id" as DivisionId,
    d."Name" as DivisionName,
    d."Code" as DivisionCode,
    c."Name" as ClassName,
    fh."Id" as FeeHeadId,
    fh."Name" as FeeHeadName,
    COUNT(DISTINCT s."Id") as TotalStudents,
    COALESCE(SUM(cfm."Amount"), 0) as TotalDueAmount,
    COALESCE(SUM(pfl."Amount"), 0) as TotalPaidAmount,
    COALESCE(SUM(cfm."Amount"), 0) - COALESCE(SUM(pfl."Amount"), 0) as TotalRemainingFees,
    ROUND(
        COALESCE(SUM(pfl."Amount"), 0)::NUMERIC / 
        NULLIF(COALESCE(SUM(cfm."Amount"), 0), 0)::NUMERIC * 100, 2
    ) as PercentagePaid
FROM 
    public."Division" d
LEFT JOIN 
    public."Class" c ON d."ClassId" = c."Id"
LEFT JOIN 
    public."Students" s ON d."Id" = s."DivisionId"
LEFT JOIN 
    public."ClassFeeMap" cfm ON c."Id" = cfm."ClassId"
LEFT JOIN 
    public."FeeHead" fh ON cfm."FeeHeadId" = fh."Id"
LEFT JOIN 
    public."Payments" p ON s."Id" = p."StudentId"
LEFT JOIN 
    public."PaymentFeeLine" pfl ON p."Id" = pfl."PaymentId" 
        AND fh."Id" = pfl."FeeHeadId"
WHERE 
    d."Id" = 'e24888e0-2f03-4de0-b6c1-19c90b449d51'
GROUP BY 
    d."Id", d."Name", d."Code", c."Name", fh."Id", fh."Name"
ORDER BY 
    fh."Name" ASC;


-- 6. PAYMENT STATUS MATRIX
-- Returns: Which students have paid for which fee heads (yes/no status)
SELECT 
    s."Id" as StudentId,
    s."FirstName" || ' ' || COALESCE(s."LastName", '') as StudentName,
    s."AdmissionNo",
    fh."Id" as FeeHeadId,
    fh."Name" as FeeHeadName,
    CASE 
        WHEN COALESCE(SUM(pfl."Amount"), 0) > 0 THEN 'PAID'
        WHEN COALESCE(SUM(cfm."Amount"), 0) > 0 THEN 'PENDING'
        ELSE 'NO FEE'
    END as PaymentStatus,
    COALESCE(cfm."Amount", 0) as DueAmount,
    COALESCE(SUM(pfl."Amount"), 0) as PaidAmount,
    COALESCE(cfm."Amount", 0) - COALESCE(SUM(pfl."Amount"), 0) as RemainingAmount
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
    s."DivisionId" = 'e24888e0-2f03-4de0-b6c1-19c90b449d51'
    AND s."IsActive" = true
GROUP BY 
    s."Id", s."FirstName", s."LastName", s."AdmissionNo",
    fh."Id", fh."Name", cfm."Amount"
ORDER BY 
    s."FirstName" ASC, fh."Name" ASC;


-- 7. OUTSTANDING FEES BY FEE HEAD (FOR COLLECTION)
-- Returns: Which fee heads have the most outstanding balance (for prioritization)
SELECT 
    fh."Id" as FeeHeadId,
    fh."Name" as FeeHeadName,
    fh."AccountCode",
    COUNT(DISTINCT s."Id") as StudentsAffected,
    COALESCE(SUM(cfm."Amount"), 0) as TotalDueAmount,
    COALESCE(SUM(pfl."Amount"), 0) as TotalPaidAmount,
    COALESCE(SUM(cfm."Amount"), 0) - COALESCE(SUM(pfl."Amount"), 0) as TotalOutstandingAmount,
    ROUND(
        COALESCE(SUM(pfl."Amount"), 0)::NUMERIC / 
        NULLIF(COALESCE(SUM(cfm."Amount"), 0), 0)::NUMERIC * 100, 2
    ) as OverallPercentagePaid
FROM 
    public."FeeHead" fh
LEFT JOIN 
    public."ClassFeeMap" cfm ON fh."Id" = cfm."FeeHeadId"
LEFT JOIN 
    public."Students" s ON cfm."ClassId" = s."ClassId"
LEFT JOIN 
    public."Payments" p ON s."Id" = p."StudentId"
LEFT JOIN 
    public."PaymentFeeLine" pfl ON p."Id" = pfl."PaymentId" 
        AND fh."Id" = pfl."FeeHeadId"
GROUP BY 
    fh."Id", fh."Name", fh."AccountCode"
ORDER BY 
    TotalOutstandingAmount DESC;


-- 8. STUDENT FEE PAYMENT COMPLIANCE REPORT
-- Returns: For each student, overall payment status and details by fee head
SELECT 
    s."Id" as StudentId,
    s."FirstName" || ' ' || COALESCE(s."LastName", '') as StudentName,
    s."AdmissionNo",
    c."Name" as ClassName,
    d."Name" as DivisionName,
    COALESCE(SUM(cfm."Amount"), 0) as TotalDueAmount,
    COALESCE(SUM(pfl."Amount"), 0) as TotalPaidAmount,
    COALESCE(SUM(cfm."Amount"), 0) - COALESCE(SUM(pfl."Amount"), 0) as TotalRemainingFees,
    ROUND(
        COALESCE(SUM(pfl."Amount"), 0)::NUMERIC / 
        NULLIF(COALESCE(SUM(cfm."Amount"), 0), 0)::NUMERIC * 100, 2
    ) as OverallPercentagePaid,
    COUNT(DISTINCT fh."Id" FILTER (WHERE COALESCE(pfl."Amount", 0) > 0)) as FeeHeadsPaid,
    COUNT(DISTINCT fh."Id" FILTER (WHERE COALESCE(pfl."Amount", 0) = 0 AND cfm."Amount" > 0)) as FeeHeadsPending,
    MAX(p."PaymentDate") as LastPaymentDate,
    COUNT(DISTINCT p."Id") as TotalPayments
FROM 
    public."Students" s
LEFT JOIN 
    public."Class" c ON s."ClassId" = c."Id"
LEFT JOIN 
    public."Division" d ON s."DivisionId" = d."Id"
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
    s."DivisionId" = 'e24888e0-2f03-4de0-b6c1-19c90b449d51'
    AND s."IsActive" = true
GROUP BY 
    s."Id", s."FirstName", s."LastName", s."AdmissionNo",
    c."Name", d."Name"
ORDER BY 
    TotalRemainingFees DESC;
