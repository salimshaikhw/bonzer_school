# BonzerSchool ERP – MVP API Documentation

**For UI / Frontend Team**

This document is the **single API reference** for the BonzerSchool MVP. Use **Section 3** for dropdown/master data, **Sections 4–8** for each module (User, Admission, Attendance, Fees, Exam), and **Section 9** for a quick endpoint list. For full fees request/response details, use **FeesModuleEndpoints.md**.

This document describes the **MVP (Minimum Viable Product)** API for **BonzerSchool**, an Enterprise School ERP supporting **nursery through higher education**. It is the single reference for what the backend exposes so the UI team can build dashboards, forms, and workflows.

---

## 1. Application Overview

| Item | Description |
|------|-------------|
| **Product** | BonzerSchool – School Management ERP |
| **Scope** | Nursery to higher education (multi-branch, multi-class) |
| **Phase** | MVP |
| **Base URL** | `https://{host}/api` (e.g. `https://api.bonzerschool.com/api`) |

### 1.1 Is This a Good MVP?

**Yes.** The MVP covers the core operations every school needs first:

| Module | Purpose | MVP coverage |
|--------|---------|--------------|
| **User & Auth** | Login, roles, password reset, registration | ✅ JWT login/refresh, register, confirm email, forgot/reset password, roles |
| **Admission** | Student lifecycle from admission to TC/transfer | ✅ CRUD, search, by roll/admission/branch/class/division, bulk upload, promote, transfer, TC |
| **Attendance** | Daily attendance, reports, leave, locks | ✅ Mark by student/class/division, reports, leave, locking, exports |
| **Fees** | Fee config, invoicing, payments, receipts | ✅ Fee heads, structures, installments, invoices, payments with fee-line split, dashboards, PDF receipt |
| **Exam** | Exams, marks, results, hall tickets | ✅ Exams, marks, exam subjects/types/components, student results, hall tickets |

Additional controllers (Branch, Class, Division, AcademicYear, Subject, Role, etc.) provide **master/reference data** required for dropdowns and filters across all modules.

---

## 2. Authentication & Conventions

### 2.1 Authentication

- **Login:** `POST /api/Auth/login` with `{ "email": "...", "password": "..." }` → returns `{ "accessToken", "refreshToken" }`.
- **Refresh:** `POST /api/Auth/refresh` with `{ "refreshToken": "..." }` → new token pair.
- **Usage:** Send the access token in the **Authorization** header:  
  `Authorization: Bearer {accessToken}`
- All endpoints under `/api/*` are **secured with `[Authorize]`** except:
  - `POST /api/Auth/login`
  - `POST /api/User/register`
  - `POST /api/User/confirm-email`
  - `POST /api/User/send-password-reset`
  - `POST /api/User/reset-password`

### 2.2 Response Conventions

- **Success:** `200 OK` with JSON body (or `201 Created` for POST create).
- **Not found:** `404 Not Found`.
- **Validation / business error:** `400 Bad Request` (message in body).
- **Unauthorized:** `401 Unauthorized`.
- Many list endpoints wrap the payload in an `ApiResponse<T>` object with a `data` property (e.g. `{ "data": [ ... ] }`). Check the actual controller response type when integrating.

### 2.3 Identifiers

- **IDs** are **GUIDs** (e.g. `"a1b2c3d4-e5f6-7890-abcd-ef1234567890"`).
- **Student identification** in the fees module uses **roll number** (and branch/class/division where needed), not student name.

---

## 3. Master / Reference Data (Dropdowns & Filters)

These endpoints supply lists needed across the app (branch, class, division, academic year, roles, subjects, etc.).

| Controller | Base route | Main use |
|------------|------------|----------|
| **Branch** | `api/Branch` | GET all, GET by id, POST, PUT – school branches |
| **AcademicYear** | `api/AcademicYear` | GET all, GET by id, POST – academic years |
| **Class** | `api/Class` | GET all, GET by id, POST, PUT – classes (e.g. Std 5, Std 6) |
| **Division** | `api/Division` | GET all, GET by id, POST, PUT – divisions (e.g. A, B) |
| **Section** | `api/Section` | GET all, GET by id, POST, PUT – sections |
| **Subject** | `api/Subject` | GET all, GET by id, POST, PUT – subjects |
| **Role** | `api/Role` | GET all, GET by id – roles (Admin, Teacher, Accountant, etc.) |
| **Staff** | `api/Staff` | GET all, GET by id, POST, PUT – staff (teachers, accountants) |
| **Medium** | `api/Medium` | GET all – medium of instruction |
| **GradeMaster** | `api/GradeMaster` | GET all – grade points (for exam results) |

**Typical usage:** On app load or per screen, call `GET /api/Branch`, `GET /api/AcademicYear`, `GET /api/Class`, `GET /api/Division`, etc., and cache for dropdowns and filters.

---

## 4. Module 1: User & Auth

### 4.1 Auth controller – `api/Auth`

| Method | Route | Purpose |
|--------|--------|---------|
| POST | `/api/Auth/login` | Login with email & password; returns access + refresh tokens |
| POST | `/api/Auth/refresh` | Get new token pair using refresh token |
| POST | `/api/Auth/revoke` | Revoke a refresh token |

**Login request body:**
```json
{ "email": "user@school.com", "password": "***" }
```

**Login/refresh response:**
```json
{ "accessToken": "eyJ...", "refreshToken": "..." }
```

### 4.2 User controller – `api/User`

| Method | Route | Purpose |
|--------|--------|---------|
| GET | `/api/User` | List all users (Admin only) |
| GET | `/api/User/{id}` | Get user by id |
| GET | `/api/User/{id}/refresh-tokens` | List refresh tokens for user |
| POST | `/api/User/register` | Register new user (AllowAnonymous) |
| POST | `/api/User/confirm-email` | Confirm email (query: `userId`, `code`) |
| POST | `/api/User/send-password-reset` | Send password reset link (query: `email`) |
| POST | `/api/User/reset-password` | Reset password (body: token, new password, etc.) |
| POST | `/api/User/send-email-verification` | Send email verification (query: `email`) |
| POST | `/api/User/change-password` | Change password (authenticated user) |
| POST | `/api/User/add-role` | Add role to user (Admin) |
| POST | `/api/User/remove-role` | Remove role from user (Admin) |
| PUT | `/api/User/{id}` | Update user |

---

## 5. Module 2: Admission (Student)

**Controller:** `api/Student`

Student lifecycle: admission → active → promote/transfer/drop/passed-out → TC. Identification by **admission number**, **roll number**, branch, class, division.

### 5.1 Core CRUD & lookup

| Method | Route | Purpose |
|--------|--------|---------|
| GET | `/api/Student` | Get all students |
| GET | `/api/Student/total` | Get total student count |
| GET | `/api/Student/{id}` | Get student by id |
| GET | `/api/Student/by-admission` | Get by admission number (query: `branchId`, `admissionNo`) |
| GET | `/api/Student/by-roll` | Get by roll number (query: `classId`, `rollNo`) |
| GET | `/api/Student/division/{divisionId}` | Students in a division |
| GET | `/api/Student/class/{classId}` | Students in a class |
| GET | `/api/Student/branch/{branchId}` | Students in a branch |
| GET | `/api/Student/academic-year/{academicYearId}` | Students in an academic year |
| POST | `/api/Student` | Create student |
| PUT | `/api/Student/{id}` | Full update |
| PATCH | `/api/Student/{id}` | Partial update |
| DELETE | `/api/Student/{id}` | Hard delete |
| DELETE | `/api/Student/{id}/soft` | Soft delete |

### 5.2 Search & filters

| Method | Route | Purpose |
|--------|--------|---------|
| POST | `/api/Student/search` | Search with filter body |
| POST | `/api/Student/search/advanced` | Advanced multi-criteria search |
| GET | `/api/Student/search/global` | Global keyword search |
| GET | `/api/Student/by-father-phone`, `by-mother-phone`, `by-guardian-phone` | By parent/guardian phone |
| GET | `/api/Student/by-gender`, `by-caste`, `by-category`, `by-religion`, etc. | By demographic filters |
| GET | `/api/Student/active`, `inactive`, `dropped`, `transferred`, `passed-out` | By status |

### 5.3 Operational

| Method | Route | Purpose |
|--------|--------|---------|
| GET | `/api/Student/today-admissions` | Admitted today |
| GET | `/api/Student/birthday-today` | Birthday today |
| GET | `/api/Student/pending-documents` | Pending documents (Aadhaar, etc.) |
| GET | `/api/Student/low-attendance` | Low attendance (default &lt; 75%) |
| GET | `/api/Student/board-exam-eligible` | Eligible for board exam |
| GET | `/api/Student/with-outstanding-fees` | Students with outstanding fees |
| POST | `/api/Student/{id}/promote` | Promote (query: `newClassId`, `academicYearId`) |
| POST | `/api/Student/{id}/transfer` | Transfer (query: `newBranchId`) |
| POST | `/api/Student/{id}/drop` | Drop student |
| POST | `/api/Student/{id}/passed-out` | Mark passed out |
| POST | `/api/Student/{id}/issue-tc` | Issue TC |
| POST | `/api/Student/{id}/restore` | Restore soft-deleted |
| POST | `/api/Student/bulk-upload` | Bulk upload (Excel) |
| GET | `/api/Student/bulk-upload/template` | Download Excel template |

### 5.4 Validation & checks

| Method | Route | Purpose |
|--------|--------|---------|
| GET | `/api/Student/check/admission` | Check if admission number exists |
| GET | `/api/Student/check/roll` | Check if roll number exists |
| GET | `/api/Student/check/aadhaar` | Check if Aadhaar exists |
| GET | `/api/Student/validate/promotion-eligibility/{id}` | Promotion eligibility |
| GET | `/api/Student/validate/transfer-eligibility/{id}` | Transfer eligibility |

Analytics and report endpoints (e.g. strength-by-class, gender-distribution, UDISE, minority reports) are also under `api/Student`; see controller for full list.

---

## 6. Module 3: Attendance

**Controller:** `api/Attendance`

Daily attendance by student/class/division; reports; leave; locking; exports.

### 6.1 Marking & CRUD

| Method | Route | Purpose |
|--------|--------|---------|
| GET | `/api/Attendance` | Get all attendance records |
| GET | `/api/Attendance/{id}` | Get by id |
| POST | `/api/Attendance` | Create attendance record |
| POST | `/api/Attendance/mark` | Mark single student attendance |
| POST | `/api/Attendance/bulk` | Bulk mark attendance |
| POST | `/api/Attendance/class/{classId}/{divisionId}` | Mark class/division (query: `date`, body: students) |
| PUT | `/api/Attendance/{id}` | Update |
| PATCH | `/api/Attendance/{attendanceId}` | Partial update |
| DELETE | `/api/Attendance/{id}` | Delete |

### 6.2 Reports (student)

| Method | Route | Purpose |
|--------|--------|---------|
| GET | `/api/Attendance/student/{studentId}/report` | Student attendance report |
| GET | `/api/Attendance/student/{studentId}/summary` | Summary |
| GET | `/api/Attendance/student/{studentId}/percentage` | Attendance percentage |
| GET | `/api/Attendance/student/{studentId}/present-count` | Present count |
| GET | `/api/Attendance/student/{studentId}/absent-count` | Absent count |
| GET | `/api/Attendance/student/{studentId}/is-low-attendance` | Is low attendance (threshold) |

### 6.3 Reports (class / division / branch)

| Method | Route | Purpose |
|--------|--------|---------|
| GET | `/api/Attendance/class/{classId}/report` | Class report |
| GET | `/api/Attendance/class/{classId}/summary` | Class summary |
| GET | `/api/Attendance/class/{classId}/percentage` | Class percentage |
| GET | `/api/Attendance/class/{classId}/low-attendance` | Low-attendance students |
| GET | `/api/Attendance/division/{divisionId}/report` | Division report |
| GET | `/api/Attendance/division/{divisionId}/summary` | Division summary |
| GET | `/api/Attendance/branch/{branchId}/report` | Branch report |
| GET | `/api/Attendance/branch/{branchId}/dashboard` | Branch dashboard |
| GET | `/api/Attendance/branch/{branchId}/today/summary` | Today’s summary for branch |

### 6.4 Locking & leave

| Method | Route | Purpose |
|--------|--------|---------|
| GET | `/api/Attendance/lock/validate` | Check if attendance locked (classId, date) |
| POST | `/api/Attendance/lock/date` | Lock by date (classId, divisionId, date) |
| POST | `/api/Attendance/unlock/date` | Unlock by date |
| POST | `/api/Attendance/leave/apply` | Apply leave |
| POST | `/api/Attendance/leave/{leaveId}/approve` | Approve leave |
| POST | `/api/Attendance/leave/{leaveId}/reject` | Reject leave |

### 6.5 Export

| Method | Route | Purpose |
|--------|--------|---------|
| GET | `/api/Attendance/export/student/{studentId}` | Export student report |
| GET | `/api/Attendance/export/class/{classId}` | Export class report |
| GET | `/api/Attendance/export/register/{classId}` | Attendance register PDF (query: month, year) |

---

## 7. Module 4: Fees

Fees module includes **configuration** (fee heads, structures, installments, class-fee mapping), **invoicing**, **payments** (with fee-head wise split), **dashboards**, and **PDF receipts**. Student identification in responses uses **roll number**.

### 7.1 Controllers overview

| Controller | Base route | Purpose |
|------------|------------|---------|
| **FeeHead** | `api/FeeHead` | Fee types (Tuition, Transport, etc.) – CRUD |
| **FeeStructure** | `api/FeeStructure` | Amount per class/division per head – CRUD |
| **ClassFeeMap** | `api/ClassFeeMap` | Class–fee mapping – CRUD |
| **FeeInstallment** | `api/FeeInstallment` | Installments (e.g. Term 1, Term 2) – CRUD |
| **Invoice** | `api/Invoice` | Student invoices – CRUD (invoice_items per fee head) |
| **Payment** | `api/Payment` | Payments – CRUD + receipt + PDF |
| **Fees** | `api/Fees` | Dashboards & analytics only (no CRUD) |

### 7.2 Key endpoints (summary)

**Configuration (CRUD):**
- `GET/POST/PUT/DELETE /api/FeeHead`, `/api/FeeHead/{id}`
- `GET/POST/PUT/DELETE /api/FeeStructure`, `/api/FeeStructure/{id}`
- `GET/POST/PUT/DELETE /api/ClassFeeMap`, `/api/ClassFeeMap/{id}`
- `GET/POST/PUT/DELETE /api/FeeInstallment`, `/api/FeeInstallment/{id}`

**Invoicing & payments:**
- `GET/POST/PUT/DELETE /api/Invoice`, `/api/Invoice/{id}`
- `GET/POST/PUT/DELETE /api/Payment`, `/api/Payment/{id}`
- `GET /api/Payment/{id}/receipt` – receipt data (roll number, class id, division id, amount, fee lines)
- `GET /api/Payment/{id}/receipt/pdf` – receipt as PDF (query: `disposition=inline` or `attachment`)

**Dashboards & analytics (all GET):**
- `GET /api/Fees/student-dashboard` – by `studentId`, `academicYearId`
- `GET /api/Fees/student-dashboard-by-roll` – by `branchId`, `classId`, **`rollNumber`**, `academicYearId`
- `GET /api/Fees/class-dashboard`, `division-dashboard`, `branch-dashboard`
- `GET /api/Fees/collection-trend`, `feehead-analytics`, `installment-analytics`
- `GET /api/Fees/outstanding-by-student`, `outstanding-by-class`, `outstanding-by-division`, `outstanding-by-branch`
- `GET /api/Fees/feehead-students` – per-fee-head status by student (uses **rollNumber** in response)

**Full fees API detail (request/response shapes, flow, DB notes):** see **[FeesModuleEndpoints.md](./FeesModuleEndpoints.md)** in this repository.

---

## 8. Module 5: Exam

Exams, subjects per exam, components, marks, results, hall tickets.

### 8.1 Controllers overview

| Controller | Base route | Purpose |
|------------|------------|---------|
| **ExamType** | `api/ExamType` | Exam types (e.g. Mid-term, Final) |
| **Exam** | `api/Exam` | Exams (linked to academic year, class, division, section) |
| **ExamSubject** | `api/ExamSubject` | Subjects for an exam (with schedule and total marks) |
| **ExamComponentType** | `api/ExamComponentType` | Component types (e.g. Theory, Practical, Viva) |
| **ExamSubjectComponent** | `api/ExamSubjectComponent` | Components per exam subject (max/pass marks, weightage) |
| **Mark** | `api/Mark` | Marks entry per student per exam subject component |
| **StudentResult** | `api/StudentResult` | Aggregated student result per exam |
| **HallTicket** | `api/HallTicket` | Hall ticket generation & fetch for student+exam |

---

### 8.2 ExamType – `api/ExamType`

**Purpose:** Manage exam types (Unit Test, Mid-Term, Final, etc.).

| Method | Route | Purpose |
|--------|--------|---------|
| GET | `/api/ExamType` | List all exam types |
| GET | `/api/ExamType/{id}` | Get exam type by id |
| POST | `/api/ExamType` | Create new exam type |
| PUT | `/api/ExamType/{id}` | Update existing exam type |
| DELETE | `/api/ExamType/{id}` | Delete exam type |

---

### 8.3 Exam – `api/Exam`

**Purpose:** Configure exams for a class/division/section in an academic year.

| Method | Route | Purpose |
|--------|--------|---------|
| GET | `/api/Exam` | List all exams |
| GET | `/api/Exam/{id}` | Get exam by id |
| POST | `/api/Exam` | Create exam (name, type, academic year, branch/class/division/section, dates) |
| PUT | `/api/Exam/{id}` | Update exam details |
| DELETE | `/api/Exam/{id}` | Delete exam |

---

### 8.4 ExamSubject – `api/ExamSubject`

**Purpose:** Attach subjects to an exam with schedule and total marks.

| Method | Route | Purpose |
|--------|--------|---------|
| GET | `/api/ExamSubject` | List all exam subjects |
| GET | `/api/ExamSubject/{id}` | Get exam subject by id |
| POST | `/api/ExamSubject` | Create exam subject (exam + subject + total marks + date/time) |
| PUT | `/api/ExamSubject/{id}` | Update exam subject |
| DELETE | `/api/ExamSubject/{id}` | Delete exam subject |

---

### 8.5 ExamComponentType – `api/ExamComponentType`

**Purpose:** Define reusable component types (Theory, Practical, Viva, Internal, etc.).

| Method | Route | Purpose |
|--------|--------|---------|
| GET | `/api/ExamComponentType` | List all component types |
| GET | `/api/ExamComponentType/{id}` | Get component type by id |
| POST | `/api/ExamComponentType` | Create component type (name, code) |
| PUT | `/api/ExamComponentType/{id}` | Update component type |
| DELETE | `/api/ExamComponentType/{id}` | Delete component type |

---

### 8.6 ExamSubjectComponent – `api/ExamSubjectComponent`

**Purpose:** Configure components (Theory/Practical/etc.) per exam subject with marks and weightage.

| Method | Route | Purpose |
|--------|--------|---------|
| GET | `/api/ExamSubjectComponent` | List all exam subject components |
| GET | `/api/ExamSubjectComponent/{id}` | Get exam subject component by id |
| POST | `/api/ExamSubjectComponent` | Create exam subject component (examSubject + component type + marks/weightage) |
| PUT | `/api/ExamSubjectComponent/{id}` | Update exam subject component |
| DELETE | `/api/ExamSubjectComponent/{id}` | Delete exam subject component |

---

### 8.7 Mark – `api/Mark`

**Purpose:** Capture marks per student per exam subject component.

| Method | Route | Purpose |
|--------|--------|---------|
| GET | `/api/Mark` | List all marks entries |
| GET | `/api/Mark/{id}` | Get marks entry by id |
| POST | `/api/Mark` | Create marks entry (student + examSubjectComponent + marks/flags) |
| PUT | `/api/Mark/{id}` | Update marks entry |
| DELETE | `/api/Mark/{id}` | Delete marks entry |

---

### 8.8 StudentResult – `api/StudentResult`

**Purpose:** Store aggregated result per student per exam.

| Method | Route | Purpose |
|--------|--------|---------|
| GET | `/api/StudentResult` | List all student results |
| GET | `/api/StudentResult/{id}` | Get student result by id |
| POST | `/api/StudentResult` | Create student result (exam + student + total/percentage/grade/rank) |
| PUT | `/api/StudentResult/{id}` | Update student result |
| DELETE | `/api/StudentResult/{id}` | Delete student result |

---

### 8.9 HallTicket – `api/HallTicket`

**Purpose:** Generate and fetch hall tickets for a student and exam.

| Method | Route | Purpose |
|--------|--------|---------|
| POST | `/api/HallTicket/generate` | Generate hall ticket for a student and exam (seat number, centre, papers) |
| GET | `/api/HallTicket/{id}` | Get hall ticket by id |
| GET | `/api/HallTicket/student/{studentId}/exam/{examId}` | Get hall ticket by student and exam (for re-print or view) |

---

### 8.10 Exam analytics – `api/ExamAnalytics`

**Purpose:** Read-only analytics for student result dashboards, growth charts, subject-wise and teacher-wise performance.

#### 8.10.1 Student exam dashboard (single exam)

- **GET** `/api/ExamAnalytics/student/{studentId}/exam/{examId}/summary`  
  **Purpose:** Get a student’s **overall result and subject-wise marks** for a selected exam (for bar chart and summary).

Example response (shape, not exact implementation):

```json
{
  "studentId": "guid",
  "examId": "guid",
  "examName": "Unit Test 3",
  "totalMarks": 480,
  "maxTotalMarks": 600,
  "percentage": 80.0,
  "grade": "A",
  "result": "PASS",
  "rank": 3,
  "subjects": [
    {
      "subjectId": "guid",
      "subjectName": "Mathematics",
      "marksObtained": 40,
      "maxMarks": 50,
      "percentage": 80.0
    }
  ]
}
```

#### 8.10.2 Student growth across exams (overall)

- **GET** `/api/ExamAnalytics/student/{studentId}/exams`  
  **Query (optional):** `examIds=guid1,guid2,...` or filters (`academicYearId`, `examTypeId`, `fromDate`, `toDate`).  
  **Purpose:** Show **overall growth per exam** (percentage, grade, result) across multiple exams (for line chart).

#### 8.10.3 Student subject-wise growth across exams

- **GET** `/api/ExamAnalytics/student/{studentId}/subject-growth`  
  **Query (optional):** `examIds=guid1,guid2,...` or filters (`examType=UNIT_TEST`, `fromDate`, `toDate`).  
  **Purpose:** Show **subject-wise growth** across multiple exams (e.g. weekly unit tests) – multi-line chart per subject.

#### 8.10.4 Exam-wise distribution for a class/division

- **GET** `/api/ExamAnalytics/exam/{examId}/class/{classId}/division/{divisionId}/distribution`  
  **Purpose:** Class/division-level analytics for a specific exam – grade buckets, pass/fail count, subject averages (for pie/bar charts).

#### 8.10.5 Subject-wise comparison across students

- **GET** `/api/ExamAnalytics/exam/{examId}/subject/{subjectId}/students`  
  **Purpose:** Compare **all students** for a subject in a given exam (marks, percentage, grade) – bar chart or toppers list.

#### 8.10.6 Teacher-wise performance analytics

- **GET** `/api/ExamAnalytics/teacher/{teacherId}/exams`  
  **Query (optional):** `academicYearId`, `classId`, `subjectId`.  
  **Purpose:** For a given teacher, show **exam-level performance** of their students (average percentage, pass rate, grade distribution) across exams they teach.

- **GET** `/api/ExamAnalytics/teacher/{teacherId}/subjects`  
  **Query (optional):** `academicYearId`, `fromDate`, `toDate`.  
  **Purpose:** Track **teacher-wise subject performance** over time – average marks/percentage per subject and exam, to see which teacher/subject is giving better results.

- **GET** `/api/ExamAnalytics/teacher/compare`  
  **Query:** `examId`, optional `subjectId`, `classId`, `divisionId`.  
  **Purpose:** Compare **multiple teachers** for a given exam (and subject/class) – average student percentage per teacher, pass rate, grade distribution (for comparison charts and management dashboards).

**Typical flow:** Create **ExamType → Exam → ExamSubject → ExamComponentType → ExamSubjectComponent**; then enter **Mark** data; compute/store **StudentResult**; generate **HallTicket** for each student+exam.

---

## 9. Quick Reference – All MVP Endpoints by Module

### User & Auth
- `POST /api/Auth/login` · `POST /api/Auth/refresh` · `POST /api/Auth/revoke`
- `GET/POST/PUT /api/User`, `GET /api/User/{id}`, `POST /api/User/register`, `POST /api/User/confirm-email`, `POST /api/User/send-password-reset`, `POST /api/User/reset-password`, `POST /api/User/change-password`, `POST /api/User/add-role`, `POST /api/User/remove-role`, `GET /api/User/{id}/refresh-tokens`

### Master data
- `api/Branch` · `api/AcademicYear` · `api/Class` · `api/Division` · `api/Section` · `api/Subject` · `api/Role` · `api/Staff` · `api/Medium` · `api/GradeMaster` (each: GET, GET/{id}, and often POST, PUT, DELETE)

### Admission (Student)
- `GET/POST/PUT/PATCH/DELETE /api/Student`, `GET /api/Student/{id}`, `GET /api/Student/total`, `GET /api/Student/by-roll`, `GET /api/Student/by-admission`, `GET /api/Student/division/{id}`, `GET /api/Student/class/{id}`, `GET /api/Student/branch/{id}`, `POST /api/Student/search`, `POST /api/Student/{id}/promote`, `POST /api/Student/{id}/transfer`, `POST /api/Student/{id}/issue-tc`, `GET /api/Student/with-outstanding-fees`, bulk upload, validation endpoints (see Section 5)

### Attendance
- `GET/POST/PUT/PATCH/DELETE /api/Attendance`, `POST /api/Attendance/mark`, `POST /api/Attendance/bulk`, `GET /api/Attendance/student/{id}/report`, `GET /api/Attendance/class/{id}/report`, `GET /api/Attendance/branch/{id}/dashboard`, lock/unlock, leave apply/approve/reject, exports (see Section 6)

### Fees
- FeeHead, FeeStructure, ClassFeeMap, FeeInstallment, Invoice, Payment – full CRUD; `GET /api/Payment/{id}/receipt`, `GET /api/Payment/{id}/receipt/pdf`; `GET /api/Fees/*` dashboards (see Section 7 and **FeesModuleEndpoints.md**)

### Exam
- ExamType, Exam, ExamSubject, ExamComponentType, ExamSubjectComponent, Mark, StudentResult – full CRUD; HallTicket GET by id, GET by student+exam, POST generate (see Section 8)

---

## 10. Appendix

### 10.1 Related documents

- **[FeesModuleEndpoints.md](./FeesModuleEndpoints.md)** – Full fees API: all request/response samples, flow (invoice → payment → receipt → PDF), roll number usage, and DB notes.

### 10.2 Not in MVP (future)

- Notifications (email/WhatsApp) – controller may exist; integration with providers may be post-MVP.
- Timetable, Homework, Certificates (Bonafide, TC, Hall Ticket) – endpoints exist; treat as supporting features for MVP or Phase 2 depending on product plan.
- Audit logs, UDISE/RTE/minority reports – available where implemented; use as needed for compliance screens.

### 10.3 Contact

For environment (base URL, auth issuer), deployment, and API versioning, check with the backend team or deployment docs.

---

*Document version: MVP 1.0 – for UI team. Last updated to align with BonzerSchool codebase (User, Admission, Attendance, Fees, Exam + master data).*

---

## 11. MVP Final Checklist (Sign-off)

Use this to confirm the API is **final for MVP** and ready for the UI team.

| # | MVP requirement | Status | Notes |
|---|-----------------|--------|--------|
| 1 | **User module** – Login, refresh, register, confirm email, forgot/reset password, change password, roles | ✅ | Auth + User controllers |
| 2 | **Admission module** – Student CRUD, search, by roll/admission/branch/class/division, promote, transfer, drop, TC, bulk upload | ✅ | StudentController |
| 3 | **Attendance module** – Mark attendance (single/bulk/class), reports (student/class/division/branch), leave, locking, exports | ✅ | AttendanceController |
| 4 | **Fees module** – Fee config (heads, structures, installments, class map), invoices, payments with fee-line split, dashboards, receipt PDF, roll number | ✅ | FeeHead, FeeStructure, ClassFeeMap, FeeInstallment, Invoice, Payment, Fees; see FeesModuleEndpoints.md |
| 5 | **Exam module** – Exam types, exams, exam subjects, components, marks, student results, hall tickets | ✅ | ExamType, Exam, ExamSubject, ExamComponentType, ExamSubjectComponent, Mark, StudentResult, HallTicket |
| 6 | **Master data** – Branch, AcademicYear, Class, Division, Section, Subject, Role, Staff (and related) for dropdowns/filters | ✅ | Section 3 |
| 7 | **API documentation** – Single reference for UI (this doc + FeesModuleEndpoints.md for fees detail) | ✅ | MVP-API-Documentation.md |

**Conclusion:** The API is **final for MVP** and **meets the defined scope** (User, Admission, Attendance, Fees, Exam + master data). The UI team can use this document and FeesModuleEndpoints.md as the authoritative API reference for the MVP release.
