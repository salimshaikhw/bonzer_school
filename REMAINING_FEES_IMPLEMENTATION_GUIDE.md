# Remaining Fees Module Implementation Guide

## Overview
This document provides a complete implementation for calculating and retrieving remaining fees for your fees module. The solution includes:

- **SQL Queries** - 7 comprehensive queries for different fee calculations
- **C# ViewModels/DTOs** - Data transfer objects for API responses
- **Service Interface** - `IRemainingFeesService` with multiple calculation methods
- **Service Implementation** - `RemainingFeesService` with full business logic
- **API Controller** - `RemainingFeesController` with REST endpoints

## Files Created/Modified

1. **REMAINING_FEES_SQL_QUERIES.sql** - Raw SQL queries for direct database queries
2. **Application/ViewModels/Fees/RemainingFeesDto.cs** - Data transfer objects
3. **Application/Abstractions/Services/IRemainingFeesService.cs** - Service interface
4. **Infrastructure/Services/RemainingFeesService.cs** - Service implementation
5. **API/Controllers/RemainingFeesController.cs** - API endpoints

## Setup Instructions

### Step 1: Register Service in Dependency Injection (Program.cs)

Add the following to your `Program.cs` or Startup configuration:

```csharp
// In your ConfigureServices or Program.cs
builder.Services.AddScoped<IRemainingFeesService, RemainingFeesService>();
```

### Step 2: Update DbContext if Needed

Ensure your `BonzerSchoolDbContext` includes the following DbSets:

```csharp
public DbSet<Student> Students { get; set; }
public DbSet<Payment> Payments { get; set; }
public DbSet<PaymentFeeLine> PaymentFeeLines { get; set; }
public DbSet<ClassFeeMap> ClassFeeMaps { get; set; }
public DbSet<FeeHead> FeeHeads { get; set; }
public DbSet<Class> Classes { get; set; }
public DbSet<Division> Divisions { get; set; }
public DbSet<Branch> Branches { get; set; }
```

### Step 3: Build and Run

```bash
dotnet build
dotnet run
```

## API Endpoints

### Student Remaining Fees
- **GET** `/api/remainingfees/student/{studentId}`
  - Returns remaining fees for a specific student

- **GET** `/api/remainingfees/student/{studentId}/details`
  - Returns detailed fee breakdown by fee head

### Class Remaining Fees
- **GET** `/api/remainingfees/class/{classId}`
  - Returns remaining fees for a specific class

- **GET** `/api/remainingfees/class/all`
  - Returns remaining fees for all classes

- **GET** `/api/remainingfees/class/{classId}/pending-students`
  - Returns students with pending fees in a class

### Division Remaining Fees
- **GET** `/api/remainingfees/division/{divisionId}`
  - Returns remaining fees for a specific division

- **GET** `/api/remainingfees/division/all`
  - Returns remaining fees for all divisions

- **GET** `/api/remainingfees/division/{divisionId}/pending-students`
  - Returns students with pending fees in a division

### Branch Remaining Fees
- **GET** `/api/remainingfees/branch/{branchId}`
  - Returns remaining fees for a specific branch

- **GET** `/api/remainingfees/branch/all`
  - Returns remaining fees for all branches

- **GET** `/api/remainingfees/branch/{branchId}/pending-students`
  - Returns students with pending fees in a branch

### Payment Summary
- **GET** `/api/remainingfees/payment-summary?monthsBack=12`
  - Returns monthly payment collection summary

### Analytics Dashboard
- **GET** `/api/remainingfees/analytics/dashboard`
  - Returns comprehensive fee analytics dashboard

- **GET** `/api/remainingfees/analytics/branch/{branchId}`
  - Returns analytics for specific branch

## Example API Responses

### Student Remaining Fees Response
```json
{
  "success": true,
  "message": "Student remaining fees retrieved successfully",
  "data": {
    "studentId": "55836f20-2f9d-4755-b64c-c755741f05a9",
    "studentName": "John Doe",
    "admissionNo": "ADM-001",
    "classId": "class-uuid",
    "divisionId": "division-uuid",
    "branchId": "branch-uuid",
    "totalDueFees": 50000.00,
    "paidAmount": 30000.00,
    "remainingFees": 20000.00,
    "percentagePaid": 60.00
  },
  "timestamp": "2026-05-03T10:30:00Z"
}
```

### Class Remaining Fees Response
```json
{
  "success": true,
  "message": "Class remaining fees retrieved successfully",
  "data": {
    "classId": "class-uuid",
    "className": "Class 10-A",
    "branchId": "branch-uuid",
    "totalStudents": 45,
    "totalClassDueFees": 2250000.00,
    "totalPaidAmount": 1350000.00,
    "totalRemainingFees": 900000.00,
    "percentagePaid": 60.00
  },
  "timestamp": "2026-05-03T10:30:00Z"
}
```

### Fee Analytics Dashboard Response
```json
{
  "success": true,
  "message": "Fee analytics dashboard retrieved successfully",
  "data": {
    "totalDueFees": 25000000.00,
    "totalPaidAmount": 15000000.00,
    "totalRemainingFees": 10000000.00,
    "overallPercentagePaid": 60.00,
    "totalStudents": 5000,
    "studentsWithPendingFees": 2000,
    "totalBranches": 10,
    "totalClasses": 100,
    "totalDivisions": 200,
    "topClassesByRemainingFees": [
      {
        "classId": "class-uuid",
        "className": "Class 10-A",
        "branchId": "branch-uuid",
        "totalStudents": 45,
        "totalClassDueFees": 2250000.00,
        "totalPaidAmount": 1350000.00,
        "totalRemainingFees": 900000.00,
        "percentagePaid": 60.00
      }
    ],
    "branchWiseSummary": [...],
    "monthlyPaymentTrend": [...]
  },
  "timestamp": "2026-05-03T10:30:00Z"
}
```

## SQL Query Examples

### Get Student Remaining Fees
```sql
SELECT 
    s."Id" as StudentId,
    s."FirstName" || ' ' || COALESCE(s."LastName", '') as StudentName,
    s."AdmissionNo",
    COALESCE(SUM(cfm."Amount"), 0) as TotalDueFees,
    COALESCE(SUM(p."Amount"), 0) as PaidAmount,
    COALESCE(SUM(cfm."Amount"), 0) - COALESCE(SUM(p."Amount"), 0) as RemainingFees
FROM public."Students" s
LEFT JOIN public."ClassFeeMap" cfm ON s."ClassId" = cfm."ClassId"
LEFT JOIN public."Payments" p ON s."Id" = p."StudentId"
WHERE s."Id" = '55836f20-2f9d-4755-b64c-c755741f05a9'
GROUP BY s."Id", s."FirstName", s."LastName", s."AdmissionNo";
```

### Get Division-wise Remaining Fees
```sql
SELECT 
    d."Id" as DivisionId,
    d."Name" as DivisionName,
    COUNT(DISTINCT s."Id") as TotalStudents,
    COALESCE(SUM(cfm."Amount"), 0) as TotalDivisionDueFees,
    COALESCE(SUM(p."Amount"), 0) as TotalPaidAmount,
    COALESCE(SUM(cfm."Amount"), 0) - COALESCE(SUM(p."Amount"), 0) as TotalRemainingFees
FROM public."Division" d
LEFT JOIN public."Students" s ON d."Id" = s."DivisionId"
LEFT JOIN public."ClassFeeMap" cfm ON d."ClassId" = cfm."ClassId"
LEFT JOIN public."Payments" p ON s."Id" = p."StudentId"
WHERE d."Id" = 'e24888e0-2f03-4de0-b6c1-19c90b449d51'
GROUP BY d."Id", d."Name";
```

## Key Features

1. **Student-level Analysis**
   - Individual student remaining fees
   - Detailed fee breakdown by fee head
   - Payment history tracking

2. **Class-level Analytics**
   - Class-wise total remaining fees
   - Number of students with pending fees
   - Payment percentage by class

3. **Division-level Analytics**
   - Division-wise remaining fees
   - Student count and payment status
   - Division-specific trends

4. **Branch-level Analytics**
   - Branch-wide remaining fees
   - Payment collection summary
   - Multi-class and multi-division overview

5. **Payment Tracking**
   - Monthly payment collection trends
   - Payment method tracking
   - Collection by staff member

6. **Dashboard Analytics**
   - Overall fee status across branches
   - Payment trends
   - Top defaulters
   - Collection efficiency metrics

## Database Schema Relationships

```
Students
├── ClassId → Class
├── DivisionId → Division
├── BranchId → Branch
└── (Payment) ← Payments

ClassFeeMap
├── ClassId → Class
├── DivisionId → Division
├── FeeHeadId → FeeHead
└── AcademicYearId → AcademicYear

Payments
├── StudentId → Students
├── BranchId → Branch
└── (PaymentFeeLine) ← PaymentFeeLines

PaymentFeeLine
├── PaymentId → Payments
└── FeeHeadId → FeeHead

Class
└── BranchId → Branch

Division
├── ClassId → Class
└── BranchId → Branch
```

## Performance Considerations

1. **Indexing Recommendations**
   ```sql
   CREATE INDEX idx_student_classid ON "Students"("ClassId");
   CREATE INDEX idx_student_divisionid ON "Students"("DivisionId");
   CREATE INDEX idx_student_branchid ON "Students"("BranchId");
   CREATE INDEX idx_payment_studentid ON "Payments"("StudentId");
   CREATE INDEX idx_classfee_classid ON "ClassFeeMap"("ClassId");
   CREATE INDEX idx_paymentline_paymentid ON "PaymentFeeLine"("PaymentId");
   ```

2. **Query Optimization Tips**
   - Use the `GetAllClassesRemainingFeesAsync` method with pagination for large datasets
   - Cache the dashboard data if it's accessed frequently
   - Consider using materialized views for complex aggregations

## Troubleshooting

### Service Not Found Error
**Error:** `InvalidOperationException: Unable to resolve service for type 'IRemainingFeesService'`

**Solution:** 
- Ensure `builder.Services.AddScoped<IRemainingFeesService, RemainingFeesService>();` is added in `Program.cs`

### DbContext Errors
**Error:** `DbSet does not contain definition for 'ClassFeeMaps'`

**Solution:**
- Verify that `BonzerSchoolDbContext` has the required DbSet properties
- Check that entity names match the database table names

### Null Reference Exceptions
**Error:** `NullReferenceException in GetStudentRemainingFeesAsync`

**Solution:**
- Verify that the student exists in the database
- Check that ClassFeeMap records exist for the student's class
- Ensure AcademicYearId is set correctly on the student

## Future Enhancements

1. **Reporting**
   - PDF reports for fee summaries
   - Excel export functionality
   - Email notifications for pending fees

2. **Automation**
   - Automatic fee calculation based on schedule
   - Reminder generation for overdue fees
   - Late fee calculations

3. **Advanced Analytics**
   - Predictive analysis for collection patterns
   - Segmentation of students by payment status
   - Trend analysis and forecasting

4. **Integration**
   - SMS/Email notifications for students
   - Integration with payment gateways
   - Reconciliation with bank statements

## Support & Documentation

For more information about the payment module, refer to:
- [FEES_MODULE_DOCUMENTATION.md](../API/FEES_MODULE_DOCUMENTATION.md)
- [MVP-API-Documentation.md](../MVP-API-Documentation.md)
- [FeesModuleEndpoints.md](../FeesModuleEndpoints.md)
