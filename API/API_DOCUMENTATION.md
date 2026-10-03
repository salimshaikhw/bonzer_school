# BonzerSchool API — Complete documentation

Single merged reference: **architecture & database**, **feature-oriented module guide**, and **alphabetical controller catalog** (all 46 controllers).

**Easier to scan in Excel?** Use the CSV files in [`spreadsheets/`](spreadsheets/) — especially `02_Controllers_Summary.csv` (one line per controller) and `01_Endpoints_All.csv` (every route; filter by **Module**). Read [`spreadsheets/00_HOW_TO_USE.txt`](spreadsheets/00_HOW_TO_USE.txt) first.

**Live `.xlsx` download (generated from code):** `GET /api/ApiDocumentation/endpoints-excel` — returns `BonzerSchool_API_Endpoints.xlsx` (controller, method name, HTTP verb, route, parameters, description). `[AllowAnonymous]`; no bearer token required.

**Table of contents:** [Introduction](#introduction) · [Swagger](#swagger-and-openapi) · [Architecture & DbContext](#1-architecture-snapshot) · [Modules 3–7](#3-login-and-registration) · [File downloads](#8-file-download-endpoints-quick-reference) · [Controller catalog](#controller-catalog-alphabetical)

---

## Introduction

Open this file in your editor or export to PDF. **Conventions:** unless noted, `[Authorize]` applies to the whole controller. **Standard CRUD** means `GET` list, `GET {id}` detail, `POST` create, `PUT {id}` update, `DELETE {id}` delete. Namespaces: mostly `API.Controllers`; `AuthController` and `UserController` use `Api.Controllers`.

---

## Swagger and OpenAPI

The API registers **Swashbuckle** (`AddSwaggerGen` with JWT Bearer). In **Development**, Swagger UI is enabled (typically `/swagger`). Use it for interactive calls and request/response shapes. This document is the stable narrative reference.

---

## 1. Architecture snapshot

| Piece | Role |
| --- | --- |
| `BonzerSchoolDbContext` | Single PostgreSQL EF Core context. Extends `IdentityDbContext<ApplicationUser, IdentityRole, string>` so ASP.NET Identity users/roles live in the same database as school domain tables. |
| `Program.cs` | Registers Npgsql, Identity on `BonzerSchoolDbContext`, JWT auth, AutoMapper, CORS, controllers with camelCase JSON. |
| Controllers | Thin HTTP layer: validate input, call application services, return HTTP status codes. Business rules live in `Application` + `Infrastructure` services. |

---

## 2. `BonzerSchoolDbContext` — main `DbSet`s and purpose

These map to PostgreSQL tables (unless noted). They are the persistence backbone for the modules below.

| DbSet property | Domain purpose |
| --- | --- |
| `academic_years` | Academic year master (date ranges, active flag). |
| `attendances` | Daily per-student attendance (`unique_attendance` on StudentId + date). |
| `attendance_date_locks` | Lock editing for a class/division/date. |
| `attendance_academic_year_locks` | Lock attendance for a whole academic year. |
| `audit_logs` | Generic audit trail (module, action). |
| `branches` | School branches; includes attendance weekly-off flags. |
| `school_holidays` | Branch calendar holidays. |
| `classes`, `divisions` | Class and section/division structure. |
| `class_fee_maps` | Maps class (+ fee structure/installment context) to amounts for a year. |
| `class_subject_maps`, `class_teacher_maps`, `teacher_subject_maps` | Curriculum and staff assignment. |
| `departments`, `mediums`, `institution_types` | Reference data. |
| `exams`, `exam_types` | Exam sessions and type (e.g. Midterm). |
| `exam_subjects`, `exam_subject_components` | Subject papers within an exam; components (max marks, weightage). |
| `exam_component_types` | Reusable component kinds (theory, practical, etc.). |
| `fee_heads`, `fee_installments`, `fee_structures` | Fee catalog: heads, installment plans, structure lines. |
| `grade_masters` | Grade bands (min/max percent → grade letter). |
| `homework`, `timetables` | Homework and timetable rows. |
| `invoices`, `invoice_items` | Fee invoices and line items. |
| `marks` | Student scores per exam subject/component. |
| `notifications` | In-app notifications. |
| `parent_users` | Parent portal linkage (e.g. username = parent email). |
| `payments`, `payment_fee_lines` | Payments and allocation to fee lines. |
| `roles` (domain `Role` entity, distinct from Identity roles if used in parallel) | Legacy/custom role table if scaffolded. |
| `school_groups` | Multi-tenant groupings. |
| `staff`, `staff_roles` | Staff directory and roles. |
| `students` | Core student master. |
| `student_results` | Aggregated exam results (if used). |
| `student_subjects` | Student–subject enrollment. |
| `subjects` | Subject master. |
| `tc_certificates`, `bonafide_certificates`, `hall_tickets` | Certificates and hall tickets. |
| `leave_requests` | Student leave applications. |
| `attendance_correction_requests` | Workflow for correcting attendance. |
| `users` (domain) | Legacy user table if present alongside Identity. |
| `RefreshTokens` | JWT refresh token storage for `AuthController`. |

Identity tables (`AspNetUsers`, etc.) are created via `IdentityDbContext` base configuration.

---

## 3. Login and registration

### 3.1 `AuthController` — route prefix `api/Auth`

| Method | HTTP | Purpose |
| --- | --- | --- |
| `Login` | POST `login` | **Anonymous.** Validates email/password via `UserManager`, returns JWT `accessToken` and `refreshToken`. Handles lockout and normalized email lookup. |
| `Refresh` | POST `refresh` | **Anonymous.** Rotates refresh token: revokes old row in `RefreshTokens`, issues new pair. |
| `Revoke` | POST `revoke` | **Anonymous.** Revokes a refresh token (logout from server side). |

### 3.2 `UserController` — route prefix `api/User`

| Method | HTTP | Auth | Purpose |
| --- | --- | --- | --- |
| `GetAll` | GET | Admin | List all application users (details DTO). |
| `Get` | GET `{id}` | Authorize | Get one user by id. |
| `GetRefreshTokens` | GET `{id}/refresh-tokens` | Authorize | List refresh tokens for a user (admin/ops). |
| `Register` | POST `register` | **AllowAnonymous** | Create Identity user + profile (`IUserService.RegisterAsync`). |
| `ConfirmEmail` | POST `confirm-email` | **AllowAnonymous** | Confirm email with query `userId` + `code`. |
| `SendPasswordReset` | POST `send-password-reset` | **AllowAnonymous** | Trigger password reset email. |
| `ResetPassword` | POST `reset-password` | **AllowAnonymous** | Complete reset with token. |
| `SendEmailVerification` | POST `send-email-verification` | Authorize | Resend verification link. |
| `ChangePassword` | POST `change-password` | Authorize | Logged-in user changes password. |
| `AddRole` / `RemoveRole` | POST `add-role` / `remove-role` | Admin | Assign or remove Identity role by name. |
| `Update` | PUT `{id}` | Authorize | Update user profile fields. |

### 3.3 `RoleController` — `api/Role`

CRUD for **domain** roles (`IRoleService`): `GetAll`, `GetById`, `Create`, `Update`, `Delete`. (Distinct from ASP.NET Identity roles unless your seed aligns them.)

### 3.4 `ParentUserController` — `api/ParentUser`

CRUD for `parent_users`: link parents to students for portal features. Methods: `GetAll`, `GetById`, `Create`, `Update`, `Delete`.

---

## 4. Admission (students)

**Primary controller:** `StudentController` — `api/Student` — **[Authorize]** on the class.

Below, endpoints are grouped by purpose. All routes are under `api/Student/...` unless stated.

### 4.1 Lists, counts, and dashboards

| Method | Route pattern | Purpose |
| --- | --- | --- |
| `GetAllStudents` | GET | Full list (service may cache). |
| `GetTotalStudents` | GET `total` | Count. |
| `GetStudentsPaged` | GET `paged` | Pagination for admission UI (`page`, `pageSize` capped). |
| `GetTodayAdmissions` | GET `today-admissions` | Students admitted today. |
| `GetStudentsWithBirthdayToday` | GET `birthday-today` | Birthdays today. |
| `GetStudentsWithPendingDocuments` | GET `pending-documents` | Missing Aadhaar / birth cert / photo. |
| `GetStudentsWithLowAttendance` | GET `low-attendance` | Below threshold % for current month. |
| `GetStudentsEligibleForBoardExam` | GET `board-exam-eligible` | Active students ready for board exam rules. |

### 4.2 Single student and “admission portfolio”

| Method | Route | Purpose |
| --- | --- | --- |
| `GetStudentAdmissionPortfolio` | GET `{id}/admission-portfolio` | **Composite:** student + attendance summary + fee dashboard (for academic year) + marks. Partial failures are logged, not fatal. |
| `GetStudentById` | GET `{id}` | Standard detail. |

### 4.3 Create / update / delete

| Method | Route | Purpose |
| --- | --- | --- |
| `CreateStudent` | POST | Create student; returns `201 Created`. |
| `GetStudentBulkUploadTemplate` | GET `bulk-upload/template` | **File download:** Excel template for bulk import. |
| `BulkUploadStudents` | POST `bulk-upload` | Multipart Excel upload; returns per-row result. |
| `UpdateStudent` | PUT `{id}` | Full replace. |
| `PatchStudent` | PATCH `{id}` | Partial update. |
| `DeleteStudent` | DELETE `{id}` | Hard delete. |
| `SoftDeleteStudent` | DELETE `{id}/soft` | `IsActive = false`. |
| `RestoreStudent` | POST `{id}/restore` | Undo soft delete. |

### 4.4 Lifecycle (status transitions)

| Method | Route | Purpose |
| --- | --- | --- |
| `ActivateStudent` | POST `{id}/activate` | Active enrollment. |
| `SuspendStudent` | POST `{id}/suspend` | Suspended. |
| `DropStudent` | POST `{id}/drop` | Dropped out. |
| `TransferStudent` | POST `{id}/transfer?newBranchId=` | Move branch. |
| `PromoteStudent` | POST `{id}/promote?newClassId=&academicYearId=` | Promote class/year. |
| `MarkStudentPassedOut` | POST `{id}/passed-out` | Completed. |
| `IssueTc` | POST `{id}/issue-tc` | TC issued workflow. |
| `ReEnrollStudent` | POST `{id}/re-enroll` | Re-enroll after exit. |
| `UpdateStudentStatus` | PATCH `{id}/status?status=` | Generic status string. |
| `UpdateLeavingDate` | PATCH `{id}/leaving-date?date=` | Leaving date. |

### 4.5 Filters (organizational)

`GetByDivision`, `GetByClass`, `GetByBranch`, `GetByAcademicYear`, `GetByClassTeacher` — filter students by hierarchy or class teacher.

### 4.6 Search and analytics (high level)

- **Search:** `POST search`, `POST search/advanced`, `GET search/global?keyword=` — flexible filters for admission desk.
- **Validation:** `check/admission`, `check/roll`, `check/aadhaar`, `validate/*`, `POST validate/duplicate` — duplicate and eligibility checks before save.
- **Analytics:** many `GET analytics/*` endpoints (strength by class/division, gender/category/caste/religion/city/state/blood/age/income distributions, growth, admission trend, dropout, promotion, retention, transfer, urban/rural, conversion).
- **Reports:** `GET reports/udise`, `minority`, `scstobc`, `aadhaar-linked`, `scholarship-eligible`, `rte-eligible` — compliance and scholarship style exports (JSON from service).

---

## 5. Attendance

### 5.1 `AttendanceController` — `api/Attendance`

**Note:** Class-level `[Authorize]` is commented out in source — **the controller may be anonymously callable** unless enforced by middleware or re-enabled. Several bulk-import actions explicitly use `[Authorize]`.

#### Core CRUD

| Method | Route | Purpose |
| --- | --- | --- |
| `GetAll` | GET | All attendance rows (use with care in production). |
| `GetById` | GET `{id}` | Single row. |
| `Create` | POST | Create from `AttendanceRequest`. |
| `Update` | PUT `{id}` | Update. |
| `Delete` | DELETE `{id}` | Delete. |

#### Marking

| Method | Route | Purpose |
| --- | --- | --- |
| `MarkAttendance` | POST `mark` | Upsert one student/day. |
| `BulkMarkAttendance` | POST `bulk` | Many rows in one payload. |
| `MarkClassAttendance` | POST `class/{classId}/{divisionId}?date=` | Whole class for a date. |
| `UpdateAttendance` | PATCH `{attendanceId}` | Partial update. |
| `SoftDeleteAttendance` / `HardDeleteAttendance` / `RestoreAttendance` | DELETE/PATCH variants | Soft/hard delete and restore. |

#### Bulk import (Excel / machine)

| Method | Route | Purpose |
| --- | --- | --- |
| `DownloadBulkAttendanceImportSample` | GET `bulk-import/sample` | **File download:** minimal sample workbook. |
| `DownloadDivisionAttendanceImportTemplate` | POST `bulk-import/division-template` | **File download:** prefilled template for class/division/date range. |
| `PreviewBulkAttendanceImport` | POST `bulk-import/preview` | JSON preview; resolves class teacher from logged-in user or maps. |
| `PreviewBulkAttendanceImportFromFile` | POST `bulk-import/preview/file` | Same, from uploaded file. |
| `BulkImportAttendanceFromMachine` | POST `bulk-import` | Commit JSON machine import. |
| `BulkImportAttendanceFromFile` | POST `bulk-import/file` | Commit from file. |
| `PreviewDivisionTemplateAttendanceImportFromFile` | POST `bulk-import/division-template/preview-file` | Preview for division-template workbook. |
| `ImportDivisionTemplateAttendanceFromFile` | POST `bulk-import/division-template/import-file` | Import division-template workbook. |

Helper logic resolves **class teacher** from: explicit `ClassTeacherId` (admins), logged-in user's email → `Staff`, or `class_teacher_map`.

#### Locks

| Method | Route | Purpose |
| --- | --- | --- |
| `LockAttendanceByDate` / `UnlockAttendanceByDate` | POST `lock/date`, `unlock/date` | Date-scoped lock for class/division. |
| `LockAttendanceForAcademicYear` / `UnlockAttendanceForAcademicYear` | POST `lock/academic-year/{id}`, `unlock/...` | Year-wide. |
| `ValidateAttendanceIsLocked` | GET `lock/validate` | Check before edits. |

#### Validations and date resolution

`resolve-date-range`, `validate/not-duplicate`, `validate/within-academic-year`, `validate/student-active`, `validate/student-class`, `validate/division-match`, `validate/holiday`, `validate/period` — guardrails for UI and imports.

#### Student-level analytics

Under `student/{studentId}/...`: detailed list, percentage, present/absent/leave/half-day counts, consecutive absence, late count, low-attendance flag, chronic absentee, trend, report, summary.

#### Division / class / branch analytics

Parallel sets of endpoints for `division`, `class`, `branch`: reports, summaries, percentages, counts, low-attendance lists, chronic absentees, trends, comparisons, heatmaps, dropout risk, attendance vs result, etc.

#### Today and dashboard

`branch/{branchId}/today/*`, `branch/{branchId}/dashboard` — operational daily KPIs.

#### Leave workflow

`leave/apply`, `leave/{id}/approve`, `leave/{id}/reject`, `leave/student/{id}/history`, `leave/class/{classId}/pending`.

#### Correction workflow

`correction/request`, `correction/{id}/approve`, `correction/{id}/reject`, `correction/class/{classId}`, `audit/{attendanceId}`.

#### Notifications (stubs or integrations)

`notify/*` — absence, consecutive absence, low attendance, chronic absentee, daily batch.

#### Export **file downloads**

| Route | Purpose |
| --- | --- |
| `export/student/{studentId}` | Student report file. |
| `export/class/{classId}` | Class report. |
| `export/division/{divisionId}` | Division report. |
| `export/branch/{branchId}` | Branch report. |
| `export/register/{classId}` | PDF attendance register (`month`, `year`). |
| `export/branch/{branchId}/excel` | Excel export. |

#### Auto / batch jobs

`auto/holiday`, `auto/close/{classId}`, `auto/calculate-percentages`, `auto/flag-low-attendance`, `auto/notify-chronic-absentees`, `auto/recalculate`, `auto/freeze`, `auto/validate` — maintenance and scheduled operations.

#### Compliance

`udise/branch/{branchId}`, `rte/branch/{branchId}` — reporting hooks.

### 5.2 `AttendanceHolidayController` — `api/AttendanceHoliday`

| Method | Purpose |
| --- | --- |
| `GetWeeklyOff` / `UpdateWeeklyOff` | Branch Saturday/Sunday (and related) weekly-off settings used when marking holidays. |
| `ListHolidays` / `AddHoliday` / `DeleteHoliday` | CRUD for `school_holidays` per branch and date range. |

---

## 6. Fees and finance

### 6.1 `FeesController` — `api/Fees`

Aggregated dashboards and analytics (uses `IFeeStudentService` + `IFeeAnalyticsService`).

| Method | Purpose |
| --- | --- |
| `GetStudentDashboard` | Per-student fee position for an academic year. |
| `GetStudentDashboardByRoll` | Same, resolved by branch + class + roll. |
| `GetClassDashboard` / `GetDivisionDashboard` / `GetBranchDashboard` | Roll-ups. |
| `GetCollectionTrend` | Payments over a date range. |
| `GetFeeHeadAnalytics` | Per fee head breakdown. |
| `GetInstallmentAnalytics` | Installment schedule performance. |
| `GetOutstandingByStudent` / `ByClass` / `ByDivision` / `ByBranch` | Arrears views. |
| `GetFeeHeadStatus` | Students' status for one fee head. |

### 6.2 `InvoiceController` — `api/Invoice`

| Method | Purpose |
| --- | --- |
| `GetAll`, `GetById` | List/detail invoices. |
| `GetStatement` | GET `{id}/statement` — full statement DTO for UI/print. |
| `Create` | Manual invoice create. |
| `CreateFromStudentSchedule` | Creates invoice from student fee schedule when appropriate (no duplicate open invoice). |
| `Update`, `Delete` | Maintain invoices. |

### 6.3 `PaymentController` — `api/Payment`

| Method | Purpose |
| --- | --- |
| `GetAll`, `GetById` | Payment list/detail (`ApiResponse` wrapper). |
| `GetReceipt` | Receipt DTO for display/reprint. |
| `GetReceiptPdf` | GET `{id}/receipt/pdf` — **PDF download** (`inline` or `attachment` via `disposition`). |
| `Create`, `Update`, `Delete` | Payment CRUD; creates allocations and updates invoice lines via service. |

### 6.4 `ParentFeesController` — `api/ParentFees`

Parent portal: Identity email must match `parent_users.Username` for parent-only routes.

| Method | Purpose |
| --- | --- |
| `GetMyStudents` | **Parent role only:** linked children. |
| `GetFeeSummary` | Fee dashboard for one student after access check (staff/parent). |
| `GetOpenInvoices` | Unpaid/partial invoices. |
| `GetPayments` | Payment history. |
| `Pay` | Record payment against invoice; updates `invoice_items` and `payment_fee_lines`. |
| `CollectPay` | Create invoice from class fee map if needed, then pay (combined flow). |

### 6.5 Master data controllers (fees setup)

These follow standard **GET/GET by id/POST/PUT/DELETE** patterns:

| Controller | Entity | Role |
| --- | --- | --- |
| `FeeHeadController` | Fee heads | Named fee categories (tuition, transport, etc.). |
| `FeeStructureController` | Fee structures | Amounts linking heads/installments/academic context. |
| `FeeInstallmentController` | Installments | Due schedule names/dates. |
| `ClassFeeMapController` | Class fee map | Class ↔ structure/amount for a year. |
| `AcademicYearController` | Academic years | Year master for all fee and attendance scoping. |

---

## 7. Exams and results

### 7.1 Exam configuration (CRUD pattern)

| Controller | Purpose |
| --- | --- |
| `ExamController` | Exams: name, dates, status, lock flags, links to type/branch/class. |
| `ExamTypeController` | Types: Term 1, Final, etc. |
| `ExamSubjectController` | Which subjects in an exam (totals, pass marks). |
| `ExamSubjectComponentController` | Splits per subject (components, max marks, weightage). |
| `ExamComponentTypeController` | Reusable component labels. |
| `GradeMasterController` | Grade boundaries for report cards. |

Each exposes: `GetAll`, `GetById`, `Create`, `Update`, `Delete`.

### 7.2 Marks and results

| Controller | Purpose |
| --- | --- |
| `MarkController` | CRUD for `marks`; `GET student/{studentId}` returns all marks for a student (newest first). |
| `StudentResultController` | Aggregated result records (`student_results`) — CRUD via `IStudentResultService`. |

### 7.3 `ExamAnalyticsController` — `api/ExamAnalytics`

| Method | Purpose |
| --- | --- |
| `GetStudentExamSummary` | One student's performance in one exam. |
| `GetStudentExamTrend` | Exam series trend with `ExamFilterDto`. |
| `GetStudentSubjectGrowth` | Subject-wise improvement. |
| `GetClassExamDistribution` | Score distribution for class/division. |
| `GetSubjectStudentComparison` | All students for a subject in an exam. |
| `GetTeacherExamPerformance` | Teacher-level exam stats. |
| `GetTeacherSubjectPerformance` | Teacher subject trends. |
| `CompareTeachers` | Cross-teacher comparison filter. |

### 7.4 `HallTicketController` — `api/HallTicket`

| Method | Purpose |
| --- | --- |
| `Generate` | Create hall ticket for student + exam. |
| `GetById` | Fetch ticket. |
| `GetByStudentAndExam` | Idempotent lookup by natural keys. |

---

## 8. File download endpoints (quick reference)

| Module | Endpoint | File |
| --- | --- | --- |
| Student | `GET api/Student/bulk-upload/template` | Excel bulk template |
| Attendance | `GET api/Attendance/bulk-import/sample` | Sample Excel |
| Attendance | `POST api/Attendance/bulk-import/division-template` | Filled template Excel |
| Attendance | `GET api/Attendance/export/...` | PDF/Excel per route |
| Payment | `GET api/Payment/{id}/receipt/pdf` | Receipt PDF |

---

## Controller catalog (alphabetical)

### Index (alphabetical)

| # | Controller | Base route |
|---|------------|------------|
| 1 | AcademicYearController | `api/AcademicYear` |
| 2 | AttendanceController | `api/Attendance` |
| 3 | AttendanceHolidayController | `api/AttendanceHoliday` |
| 4 | AuditLogController | `api/AuditLog` |
| 5 | AuthController | `api/Auth` |
| 6 | BonafideCertificateController | `api/BonafideCertificate` |
| 7 | BranchController | `api/Branch` |
| 8 | ClassController | `api/Class` |
| 9 | ClassFeeMapController | `api/ClassFeeMap` |
| 10 | ClassSubjectMapController | `api/ClassSubjectMap` |
| 11 | ClassTeacherMapController | `api/ClassTeacherMap` |
| 12 | DepartmentController | `api/Department` |
| 13 | DivisionController | `api/Division` |
| 14 | ExamAnalyticsController | `api/ExamAnalytics` |
| 15 | ExamComponentTypeController | `api/ExamComponentType` |
| 16 | ExamController | `api/Exam` |
| 17 | ExamSubjectComponentController | `api/ExamSubjectComponent` |
| 18 | ExamSubjectController | `api/ExamSubject` |
| 19 | ExamTypeController | `api/ExamType` |
| 20 | FeeHeadController | `api/FeeHead` |
| 21 | FeeInstallmentController | `api/FeeInstallment` |
| 22 | FeeStructureController | `api/FeeStructure` |
| 23 | FeesController | `api/Fees` |
| 24 | GradeMasterController | `api/GradeMaster` |
| 25 | HallTicketController | `api/HallTicket` |
| 26 | HomeworkController | `api/Homework` |
| 27 | InstitutionTypeController | `api/InstitutionType` |
| 28 | InvoiceController | `api/Invoice` |
| 29 | MarkController | `api/Mark` |
| 30 | MediumController | `api/Medium` |
| 31 | NotificationController | `api/Notification` |
| 32 | ParentFeesController | `api/ParentFees` |
| 33 | ParentUserController | `api/ParentUser` |
| 34 | PaymentController | `api/Payment` |
| 35 | RoleController | `api/Role` |
| 36 | SchoolGroupController | `api/SchoolGroup` |
| 37 | StaffController | `api/Staff` |
| 38 | StaffRoleController | `api/StaffRole` |
| 39 | StudentController | `api/Student` |
| 40 | StudentResultController | `api/StudentResult` |
| 41 | StudentSubjectController | `api/StudentSubject` |
| 42 | SubjectController | `api/Subject` |
| 43 | TcCertificateController | `api/TcCertificate` |
| 44 | TeacherSubjectMapController | `api/TeacherSubjectMap` |
| 45 | TimetableController | `api/Timetable` |
| 46 | UserController | `api/User` |

---

## 1. AcademicYearController

**Purpose:** Manage academic years (start/end dates, naming, active flag) used to scope fees, attendance, promotions, and exams.

**Auth:** `[Authorize]` on controller.

| Method | Route | Purpose |
|--------|-------|---------|
| GetAll | GET | List all academic years. |
| GetById | GET `{id}` | Single year. |
| Create | POST | Create year. |
| Update | PUT `{id}` | Update year. |
| Delete | DELETE `{id}` | Delete (handles "not found" in catch). |

---

## 2. AttendanceController

**Purpose:** Full attendance lifecycle: daily marks, bulk import (JSON + Excel/CSV), locks, validation, student/class/division/branch analytics, leave and correction workflows, exports (files), auto jobs, UDISE/RTE hooks. Resolves class teacher for bulk import from logged-in staff or `class_teacher_map`.

**Auth:** `[Authorize]` on the class is **commented out** — many endpoints are effectively open unless middleware enforces auth; bulk-import preview/commit routes **use `[Authorize]`** explicitly.

**Representative routes** (grouped):

| Group | Routes (relative to `api/Attendance`) | Purpose |
|-------|----------------------------------------|---------|
| CRUD | GET, GET `{id}`, POST, PUT `{id}`, DELETE `{id}` | Raw attendance rows. |
| Mark | POST `mark`, POST `bulk`, POST `class/{classId}/{divisionId}?date=` | Single, bulk, class roster. |
| Bulk file | GET `bulk-import/sample` | **Download** sample Excel. |
| | POST `bulk-import/division-template` | **Download** filled division template. |
| | POST `bulk-import/preview`, `preview/file` | Preview without save. |
| | POST `bulk-import`, `file`, `division-template/preview-file`, `import-file` | Commit import. |
| Row ops | PATCH `{attendanceId}`, DELETE `{attendanceId}/soft\|hard`, POST `{attendanceId}/restore` | Update / soft-delete / hard-delete / restore. |
| Locks | POST `lock/date`, `unlock/date`, `lock/academic-year/{id}`, `unlock/...`, GET `lock/validate` | Prevent edits after lock. |
| Validation | `resolve-date-range`, `validate/not-duplicate`, `within-academic-year`, `student-active`, `student-class`, `division-match`, `holiday`, `period` | UI and server checks. |
| Student | `student/{studentId}/detailed`, `percentage`, `present-count`, `absent-count`, `leave-count`, `halfday-count`, `consecutive-absence-days`, `late-count`, `is-low-attendance`, `is-chronic-absentee`, `trend`, `report`, `summary` | Per-student analytics. |
| Division | `division/{divisionId}/report`, `summary`, `detailed`, `percentage`, counts, `low-attendance`, `chronic-absentees`, `trend` | Section-level. |
| Branch | `branch/{branchId}/report`, `summary`, `percentage`, `low-attendance`, `chronic-absentees`, `trend`, `comparison-classes`, `gender-distribution`, `category-distribution`, `today/*`, `dashboard` | Branch KPIs. |
| Class | `class/{classId}/report`, `summary`, `detailed`, `percentage`, counts, `low-attendance`, `chronic-absentees`, `trend`, `compare-months`, `dropout-risk`, `attendance-vs-result`, `top-regular`, `heatmap` | Class-level. |
| Leave | POST `leave/apply`, `leave/{id}/approve`, `leave/{id}/reject`, GET `leave/student/{id}/history`, `leave/class/{classId}/pending` | Leave management. |
| Correction | POST `correction/request`, `correction/{id}/approve`, `reject`, GET `correction/class/{classId}`, POST `audit/{attendanceId}` | Correction workflow. |
| Notify | POST `notify/absence`, `consecutive-absence`, `low-attendance`, `chronic-absentee`, `daily-absence` | Notification triggers. |
| Export | GET `export/student/{id}`, `export/class/{id}`, `export/division/{id}`, `export/branch/{id}`, `export/register/{classId}`, `export/branch/{branchId}/excel` | **File** reports. |
| Auto | POST `auto/holiday`, `close/{classId}`, `calculate-percentages`, `flag-low-attendance`, `notify-chronic-absentees`, `recalculate`, `freeze`, GET `auto/validate` | Batch maintenance. |
| Compliance | GET `udise/branch/{branchId}`, `rte/branch/{branchId}` | Regulatory reports. |

---

## 3. AttendanceHolidayController

**Purpose:** Branch-level weekly-off configuration and school calendar holidays (`school_holidays`) used when marking attendance.

| Method | Route | Purpose |
|--------|-------|---------|
| GetWeeklyOff | GET `branch/{branchId}/weekly-off` | Read Saturday/Sunday (and related) settings. |
| UpdateWeeklyOff | PUT `branch/{branchId}/weekly-off` | Update weekly-off. |
| ListHolidays | GET `branch/{branchId}/holidays` | Optional `from` / `to` filter. |
| AddHoliday | POST `branch/{branchId}/holidays` | Add holiday. |
| DeleteHoliday | DELETE `{id}` | Remove holiday. |

---

## 4. AuditLogController

**Purpose:** Create and query audit log entries (`audit_logs`) for compliance and debugging.

**Pattern:** Standard CRUD — `GetAll`, `GetById`, `Create`, `Update`, `Delete`.

---

## 5. AuthController

**Purpose:** JWT login and refresh-token rotation (no registration here — see `UserController`).

**Auth:** `[AllowAnonymous]` on controller.

| Method | Route | Purpose |
|--------|-------|---------|
| Login | POST `login` | Body: `email`, `password`. Returns `accessToken`, `refreshToken`. |
| Refresh | POST `refresh` | Body: `{ refreshToken }`. Revokes old token, issues new pair. |
| Revoke | POST `revoke` | Invalidate refresh token. |

---

## 6. BonafideCertificateController

**Purpose:** Issue and retrieve bonafide (study) certificates for students.

| Method | Route | Purpose |
|--------|-------|---------|
| Create | POST | Create certificate record. |
| GetById | GET `{id}` | Single certificate. |
| GetByStudent | GET `student/{studentId}` | List by student. |

---

## 7. BranchController

**Purpose:** School branches (campuses) — contact info, address, attendance flags, etc.

**Auth:** `[Authorize]`; **Create/Update/Delete** require **Admin**.

| Method | Route | Purpose |
|--------|-------|---------|
| GetAll | GET | All branches. |
| GetById | GET `{id}` | One branch. |
| Create | POST | Admin only. |
| Update | PUT `{id}` | Admin only. |
| Delete | DELETE `{id}` | Admin only. |

---

## 8. ClassController

**Purpose:** Manage classes (grades/levels) within branches.

**Auth:** Create/Update/Delete — **Admin**.

| Method | Route | Purpose |
|--------|-------|---------|
| GetAll | GET | All classes. |
| GetByBranch | GET `by-branch/{branchId}` | Classes for a branch. |
| GetById | GET `{id}` | Single class. |
| Create | POST | Admin. |
| Update | PUT `{id}` | Admin. |
| Delete | DELETE `{id}` | Admin. |

---

## 9. ClassFeeMapController

**Purpose:** Map a class (and academic year context) to fee structure amounts — drives fee generation and parent "collect pay" flows.

**Pattern:** Standard CRUD.

---

## 10. ClassSubjectMapController

**Purpose:** Map subjects to classes for a given academic year (curriculum).

**Pattern:** Standard CRUD.

---

## 11. ClassTeacherMapController

**Purpose:** Assign class teachers to class/division (and academic year) — used by attendance bulk import to resolve staff.

**Pattern:** Standard CRUD.

---

## 12. DepartmentController

**Purpose:** Organizational departments (e.g. Science, Admin).

**Pattern:** Standard CRUD.

---

## 13. DivisionController

**Purpose:** Sections/divisions within a class (e.g. A, B).

| Method | Route | Purpose |
|--------|-------|---------|
| GetAll | GET | All divisions. |
| GetByClass | GET `by-class/{classId}` | Divisions for a class. |
| GetById | GET `{id}` | One division. |
| Create / Update / Delete | POST, PUT `{id}`, DELETE `{id}` | CRUD. |

---

## 14. ExamAnalyticsController

**Purpose:** Analytics on exam performance — student trends, class distributions, teacher comparisons.

| Method | Route | Purpose |
|--------|-------|---------|
| GetStudentExamSummary | GET `student/{studentId}/exam/{examId}/summary` | One summary. |
| GetStudentExamTrend | GET `student/{studentId}/exams` | Filter: `ExamFilterDto`. |
| GetStudentSubjectGrowth | GET `student/{studentId}/subject-growth` | Subject growth. |
| GetClassExamDistribution | GET `exam/{examId}/class/{classId}/division/{divisionId}/distribution` | Score distribution. |
| GetSubjectStudentComparison | GET `exam/{examId}/subject/{subjectId}/students` | Per-student in subject. |
| GetTeacherExamPerformance | GET `teacher/{teacherId}/exams` | `TeacherExamFilterDto`. |
| GetTeacherSubjectPerformance | GET `teacher/{teacherId}/subjects` | `TeacherSubjectFilterDto`. |
| CompareTeachers | GET `teacher/compare` | `TeacherComparisonFilterDto`. |

---

## 15. ExamComponentTypeController

**Purpose:** Master list of exam component types (Theory, Practical, etc.).

**Pattern:** Standard CRUD.

---

## 16. ExamController

**Purpose:** Exam sessions (name, schedule, branch/class scope, lock status).

**Pattern:** Standard CRUD.

---

## 17. ExamSubjectComponentController

**Purpose:** Components within an exam subject (marks, weightage, pass marks).

**Pattern:** Standard CRUD.

---

## 18. ExamSubjectController

**Purpose:** Links an exam to a subject with total/max/pass marks.

**Pattern:** Standard CRUD.

---

## 19. ExamTypeController

**Purpose:** Types of exams (e.g. Term 1, Annual) — referenced by exams.

**Pattern:** Standard CRUD.

---

## 20. FeeHeadController

**Purpose:** Fee categories (Tuition, Transport, Lab, etc.).

**Pattern:** Standard CRUD.

---

## 21. FeeInstallmentController

**Purpose:** Installment definitions (names, due dates) tied to fee planning.

**Pattern:** Standard CRUD.

---

## 22. FeeStructureController

**Purpose:** Structured fee definitions (amounts linking heads, installments, academic context).

**Pattern:** Standard CRUD.

---

## 23. FeesController

**Purpose:** **Aggregated fee dashboards and analytics** (not raw CRUD). Uses `IFeeStudentService` and `IFeeAnalyticsService`.

| Method | Route | Purpose |
|--------|-------|---------|
| GetStudentDashboard | GET `student-dashboard` | Query: `studentId`, `academicYearId`. |
| GetStudentDashboardByRoll | GET `student-dashboard-by-roll` | Roll lookup within branch/class. |
| GetClassDashboard | GET `class-dashboard` | Class roll-up. |
| GetDivisionDashboard | GET `division-dashboard` | Division roll-up. |
| GetBranchDashboard | GET `branch-dashboard` | Branch roll-up. |
| GetCollectionTrend | GET `collection-trend` | Date range collection. |
| GetFeeHeadAnalytics | GET `feehead-analytics` | Optional class/division filter. |
| GetInstallmentAnalytics | GET `installment-analytics` | Installment performance. |
| GetOutstandingByStudent / Class / Division / Branch | GET `outstanding-by-*` | Arrears. |
| GetFeeHeadStatus | GET `feehead-students` | Status per fee head. |

---

## 24. GradeMasterController

**Purpose:** Grade bands (min/max percent → grade letter) for report cards.

**Pattern:** Standard CRUD.

---

## 25. HallTicketController

**Purpose:** Generate and fetch exam hall tickets for students.

| Method | Route | Purpose |
|--------|-------|---------|
| Generate | POST `generate` | Create hall ticket. |
| GetById | GET `{id}` | By id. |
| GetByStudentAndExam | GET `student/{studentId}/exam/{examId}` | Natural key lookup. |

---

## 26. HomeworkController

**Purpose:** Homework assignments (class/subject/date).

**Pattern:** Standard CRUD.

---

## 27. InstitutionTypeController

**Purpose:** Reference data for institution types.

**Pattern:** Standard CRUD.

---

## 28. InvoiceController

**Purpose:** Fee invoices and statements; optional creation from student fee schedule.

| Method | Route | Purpose |
|--------|-------|---------|
| GetAll | GET | All invoices. |
| GetStatement | GET `{id}/statement` | Full statement DTO. |
| GetById | GET `{id}` | Detail. |
| Create | POST | Manual invoice. |
| CreateFromStudentSchedule | POST `from-student-schedule` | Build from schedule when no open invoice. |
| Update | PUT `{id}` | Update. |
| Delete | DELETE `{id}` | Delete. |

---

## 29. MarkController

**Purpose:** Student marks per exam subject component.

| Method | Route | Purpose |
|--------|-------|---------|
| GetAll | GET | All marks. |
| GetByStudentId | GET `student/{studentId}` | All marks for student (newest first). |
| GetById | GET `{id}` | Single row. |
| Create | POST | Create mark. |
| Update | PUT `{id}` | Update. |
| Delete | DELETE `{id}` | Delete. |

---

## 30. MediumController

**Purpose:** Medium of instruction (e.g. English, Kannada).

**Pattern:** Standard CRUD (returns `Ok` on delete).

---

## 31. NotificationController

**Purpose:** In-app notifications.

**Pattern:** Standard CRUD.

---

## 32. ParentFeesController

**Purpose:** Parent (and authorized staff) portal for fees: linked students, fee summary, invoices, payment history, record payment, combined collect+pay.

**Notes:** `GetMyStudents` requires **Parent** role. Other actions use `IParentFeeService` to verify the caller may access the student.

| Method | Route | Purpose |
|--------|-------|---------|
| GetMyStudents | GET `my-students` | Parent only: children linked by email. |
| GetFeeSummary | GET `students/{studentId}/fee-summary` | Same data as `Fees/student-dashboard` after access check. |
| GetOpenInvoices | GET `students/{studentId}/open-invoices` | Unpaid/partial invoices. |
| GetPayments | GET `students/{studentId}/payments` | Payment history. |
| Pay | POST `students/{studentId}/pay` | Pay against invoice + fee lines. |
| CollectPay | POST `students/{studentId}/collect-pay` | Create invoice if needed, then pay. |

---

## 33. ParentUserController

**Purpose:** CRUD for `parent_users` (link parent identity to student records).

**Pattern:** Standard CRUD — `GetAll`, `GetById`, `Create`, `Update`, `Delete`.

---

## 34. PaymentController

**Purpose:** Record fee payments; receipt JSON and **PDF**.

**Pattern:** List/get/create/update/delete wrapped in `ApiResponse<object>`.

| Method | Route | Purpose |
|--------|-------|---------|
| GetReceipt | GET `{id}/receipt` | Receipt DTO for UI/print. |
| GetReceiptPdf | GET `{id}/receipt/pdf` | **PDF** (`disposition` = inline or attachment). |

---

## 35. RoleController

**Purpose:** Domain **Role** entity CRUD (application roles table), distinct from ASP.NET Identity roles unless aligned by seed.

**Pattern:** Standard CRUD.

---

## 36. SchoolGroupController

**Purpose:** Multi-tenant / school group management.

**Auth:** Create/Delete — **Admin**; Update is **not** restricted to Admin in code (commented alternate Update exists).

| Method | Route | Purpose |
|--------|-------|---------|
| GetAll / GetById | GET, GET `{id}` | Read. |
| Create | POST | Admin. |
| Update | PUT `{id}` | Update group. |
| Delete | DELETE `{id}` | Admin. |

---

## 37. StaffController

**Purpose:** Staff directory (name, branch, email — email often links to Identity for attendance).

**Pattern:** Standard CRUD.

---

## 38. StaffRoleController

**Purpose:** Junction or assignment of roles to staff (`staff_roles`).

**Pattern:** Standard CRUD.

---

## 39. StudentController

**Purpose:** **Primary student admission and lifecycle API**: CRUD, bulk Excel **template download** + upload, composite admission portfolio, dozens of filters, analytics, compliance reports, validation.

**Auth:** `[Authorize]` on controller.

**Categories** (all under `api/Student`):

| Category | Example routes | Purpose |
|----------|------------------|---------|
| Lists | GET, `total`, `paged`, `today-admissions`, `birthday-today`, `pending-documents`, `low-attendance`, `board-exam-eligible` | Operational lists. |
| Portfolio | GET `{id}/admission-portfolio` | Student + attendance + fees + marks. |
| CRUD | GET `{id}`, POST, PUT `{id}`, PATCH `{id}`, DELETE `{id}`, `soft`, POST `restore` | Full lifecycle. |
| Bulk | GET `bulk-upload/template` (**file**), POST `bulk-upload` | Template + Excel import. |
| Lifecycle | POST `{id}/activate`, `suspend`, `drop`, `transfer`, `promote`, `passed-out`, `issue-tc`, `re-enroll`, PATCH `status`, `leaving-date` | Status transitions. |
| Org filters | `division/{id}`, `class/{id}`, `branch/{id}`, `academic-year/{id}`, `class-teacher/{staffId}` | Scope by hierarchy. |
| Demographic / contact | `aadhaar/{aadhaar}`, `by-admission`, `by-roll`, `by-father-phone`, `by-mother-phone`, `by-guardian-phone`, `by-email`, `by-gender`, `by-caste`, "¦ | Search and filter. |
| Status / special | `with-outstanding-fees`, `with-scholarship`, `with-medical-condition`, `with-allergies`, `by-height-range`, `by-weight-range`, `by-dob-range`, `by-admission-date-range`, `by-leaving-date-range`, `by-status`, `active`, `inactive`, `dropped`, `transferred`, `passed-out` | Cohort queries. |
| Search | POST `search`, POST `search/advanced`, GET `search/global` | Search DTOs. |
| Analytics | GET `analytics/*` (strength, distributions, trends, dropout, promotion, retention, transfer, urban-rural, conversion) | Branch-scoped analytics (`branchId` query where required). |
| Reports | GET `reports/udise`, `minority`, `scstobc`, `aadhaar-linked`, `scholarship-eligible`, `rte-eligible` | Compliance-style reports. |
| Checks | GET `check/admission`, `check/roll`, `check/aadhaar` | `{ exists: bool }`. |
| Validation | GET `validate/aadhaar`, `validate/age-eligibility`, POST `validate/duplicate`, GET `validate/promotion-eligibility/{id}`, `validate/transfer-eligibility/{id}` | Pre-save validation. |

---

## 40. StudentResultController

**Purpose:** Aggregated **student result** records (e.g. final grade per exam/year).

**Pattern:** Standard CRUD (returns `Ok` for delete).

---

## 41. StudentSubjectController

**Purpose:** Student enrollment in optional/elective subjects.

**Pattern:** Standard CRUD.

---

## 42. SubjectController

**Purpose:** Subject master data.

**Pattern:** Standard CRUD.

---

## 43. TcCertificateController

**Purpose:** Transfer Certificate (TC) — create, read, and **workflow** (clearances, approvals, issue, reject).

| Method | Route | Purpose |
|--------|-------|---------|
| Create | POST | Create TC request. |
| GetById | GET `{id}` | Single TC. |
| GetByStudent | GET `student/{studentId}` | List by student. |
| Initiate | POST `{id}/initiate` | Start clearance workflow. |
| HeadClerkApprove | POST `{id}/head-clerk-approve?staffId=` | Workflow step. |
| ClassTeacherApprove | POST `{id}/class-teacher-approve?staffId=` | Workflow step. |
| PrincipalApprove | POST `{id}/principal-approve?staffId=` | Workflow step. |
| Issue | POST `{id}/issue?staffId=` | Final issue. |
| Reject | POST `{id}/reject?staffId=` | Body: reason string. |

---

## 44. TeacherSubjectMapController

**Purpose:** Which teacher teaches which subject (often branch/class scoped in service).

**Pattern:** Standard CRUD.

---

## 45. TimetableController

**Purpose:** Class timetable slots (period, subject, teacher).

**Pattern:** Standard CRUD.

---

## 46. UserController

**Purpose:** User registration, identity profile, password and email flows, **ASP.NET Identity** role assignment.

**Auth:** Mixed — `register` and password reset are **AllowAnonymous**; listing users and role ops require **Admin** / **Authorize** as marked in source.

| Method | Route | Purpose |
|--------|-------|---------|
| GetAll | GET | Admin — all users. |
| Get | GET `{id}` | User details. |
| GetRefreshTokens | GET `{id}/refresh-tokens` | Refresh tokens for user. |
| Register | POST `register` | **Anonymous** registration. |
| ConfirmEmail | POST `confirm-email` | Query `userId`, `code`. |
| SendPasswordReset | POST `send-password-reset` | Query `email`. |
| ResetPassword | POST `reset-password` | Body `ResetPasswordInput`. |
| SendEmailVerification | POST `send-email-verification` | Query `email`. |
| ChangePassword | POST `change-password` | Logged-in user. |
| AddRole / RemoveRole | POST `add-role`, `remove-role` | Admin. |
| Update | PUT `{id}` | Update profile. |

---

## File download endpoints (summary)

| Controller | Route | Content |
|------------|-------|---------|
| StudentController | GET `api/Student/bulk-upload/template` | Excel bulk template |
| AttendanceController | GET `api/Attendance/bulk-import/sample` | Sample Excel |
| AttendanceController | POST `api/Attendance/bulk-import/division-template` | Filled Excel |
| AttendanceController | GET `api/Attendance/export/*` | Reports/PDF/Excel per sub-route |
| PaymentController | GET `api/Payment/{id}/receipt/pdf` | Receipt PDF |
