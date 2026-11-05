# Module 1: School setup (top-down) + Students: test guide for Visual Studio

This guide builds a school from nothing, in the order the system requires:

```
Super Admin (platform)
  └─ School Group  (= the tenant: one row in table `organizations`, one database)
       ├─ Group Admin login
       ├─ Academic years
       ├─ User logins + roles (Principal, Teacher, Accountant…)
       └─ Branches (many per school group: schools / campuses)
            ├─ Staff records (linked to logins by email)
            ├─ Classes ── Divisions
            ├─ Subjects ── class-subject / class-teacher mapping
            └─ Students ── Parent link
```

Companion file: [01-School-Setup-Hierarchy.http](01-School-Setup-Hierarchy.http) contains every request below, runnable inside Visual Studio.

---

## Part 0: How the databases work (read this first)

There is **one common database** plus **one database per school group**, all on the same PostgreSQL server.

```
PostgreSQL server (localhost:5433)
│
├── bonzer_catalog               ← COMMON database (one per installation)
│     ├── organizations            the school groups (= tenants) + the name of each one's database
│     ├── AspNetUsers              ALL logins of ALL school groups (each row has tenant_id)
│     ├── AspNetRoles / UserRoles  roles (SuperAdmin, GroupAdmin, Principal, Teacher…)
│     ├── refresh_tokens
│     ├── institution_types        shared lookups (Pre-Primary, School, Junior College)
│     ├── mediums                  shared lookups (English, Semi-English, Marathi, Hindi)
│     └── OpenIddict*              OAuth clients for integrations
│
├── bonzer_school_demo           ← school group "demo" (auto-created in Development)
├── bonzer_school_sunrise_trust  ← school group "sunrise-trust" (you create it in Step 3)
│     ├── (no school group table here: the group is the row in `organizations`)
│     ├── branches, academic_years, classes, divisions, subjects
│     ├── staff, students, parent_users
│     ├── attendance, fees, exams, marks, certificates …
│     └── seeded defaults: exam types, grades, fee heads, departments
└── …
```

### Key rules

| Question | Answer |
|---|---|
| Is there a common database? | **Yes, `bonzer_catalog`.** It holds the school groups (`organizations` table), **all logins and roles**, and shared lookups. It holds no school data. |
| When is a school group's database created? | When the Super Admin calls `POST /api/SchoolGroup`. In that one call the API inserts the `organizations` row, creates the database `bonzer_school_<identifier>` (hyphens become `_`), runs all migrations, seeds defaults and creates the first **Group Admin** login. |
| What if it fails halfway? | Everything is rolled back: the database, the login and the catalog row are removed. |
| Is there a separate tenant table? | **No.** The school group is the tenant: **1 school group = 1 `organizations` row = 1 database**, and it has **many branches** (`branches.group_id` holds its id). |
| How does a request know which database to use? | The login's JWT carries a `tenant_id` claim. The API reads it on every request and connects to that tenant's database. Users cannot switch tenant. |
| And the Super Admin? | Has no tenant. To look inside a school group they send the header `X-Tenant: <identifier>`. |
| Where are passwords for the DB stored? | Nowhere in the catalog. The server, user and password come from the `PostgresConnection` setting; only database **names** are stored. |
| What happens to databases on startup? | The API migrates `bonzer_catalog` and **every active tenant database** automatically. There is no manual migration step. |
| Are roles created by me? | No. The roles are **fixed** and seeded at startup. You **assign** them to logins. |

### Fixed roles

| Level | Roles |
|---|---|
| Platform | `SuperAdmin` |
| School group | `GroupAdmin` |
| Branch office | `BranchAdmin` |
| Leadership | `Principal`, `VicePrincipal`, `Hod` |
| Teaching | `Teacher`, `ClassTeacher`, `SportsTeacher` |
| Other staff | `Accountant`, `Librarian` |
| Family | `Parent`, `Student` |

### Who may do what (used in the steps below)

| Action | Allowed roles |
|---|---|
| Create / deactivate school group, edit institution types and mediums | SuperAdmin |
| Edit own school group, create branches | GroupAdmin |
| Create academic years, register users, assign roles | GroupAdmin, BranchAdmin |
| Create classes, subjects | GroupAdmin, BranchAdmin, Principal, VicePrincipal, Hod |
| Create / change students | GroupAdmin, BranchAdmin, Principal, VicePrincipal, ClassTeacher |
| Read anything in own school group | Any logged-in user |

---

## Run sheet: every API call, in order, until configuration is complete

Base URL `http://localhost:5102`. All requests send `Content-Type: application/json`, and every request except login sends `Authorization: Bearer <token>`.
**Save each returned `id`**: the "Needs" column shows which earlier ids a call uses. Details of each call are in the Part named in the last column.

| # | Login as | Method + URL | Body (key fields) | Needs | Saves | Details |
|---|---|---|---|---|---|---|
| 1 | none | `POST /api/Auth/login` | superadmin@bonzer.local / Admin@123 | | token (SuperAdmin) | Step 1 |
| 2 | SuperAdmin | `GET /api/InstitutionType` | | | institutionTypeId | Step 2 |
| 3 | SuperAdmin | `GET /api/Medium` | | | mediumId | Step 2 |
| 4 | SuperAdmin | `POST /api/SchoolGroup` | identifier, name, code, adminEmail, adminPassword… | | schoolGroupId (= tenant id) | Step 3 |
| 5 | none | `POST /api/Auth/login` | admin@sunrise.test / Sunrise@123 | | token (GroupAdmin) | Step 4 |
| 6 | GroupAdmin | `GET /api/Department` | | | deptId | Step 4 |
| 7 | GroupAdmin | `POST /api/Branch` | institutionTypeId, mediumId, schooluid, name… | 2, 3 | branchId (+ branch2Id) | Step 5 |
| 8 | GroupAdmin | `POST /api/AcademicYear` | yearName, startDate, endDate, isActive | | academicYearId | Step 6 |
| 9 | GroupAdmin | `POST /api/User/register` ×6 | email, password, role | | | Step 7 |
| 10 | GroupAdmin | `POST /api/Staff` (per staff login) | branchId, deptId, email… | 6, 7 | staffId | Step 8 |
| 11 | GroupAdmin | `POST /api/Class` ×3 | branchId, name | 7 | classId, higherClassId | Step 9 |
| 12 | GroupAdmin | `POST /api/Division` ×2 | classId, branchId, name, code, availableSeats | 7, 11 | divisionId | Step 10 |
| 13 | GroupAdmin | `POST /api/Subject` ×3 | name, code | | subjectId | Step 11 |
| 14 | GroupAdmin | `POST /api/ClassSubjectMap` | classId, divisionId, subjectId, academicYear | 11–13 | | Step 11 |
| 15 | GroupAdmin | `POST /api/ClassTeacherMap` | classId, divisionId, staffId, academicYear | 10–12 | | Step 11 |
| 16 | GroupAdmin | `POST /api/TeacherSubjectMap` | staffId, subjectId, classId, divisionId, academicYear | 10–13 | | Step 11b |
| 17 | GroupAdmin | `PUT /api/AttendanceHoliday/branch/{branchId}/weekly-off` | saturdayPattern, sundayOff | 7 | | Step 18 |
| 18 | GroupAdmin | `POST /api/AttendanceHoliday/branch/{branchId}/holidays` | holidayDate, name | 7 | | Step 18 |
| 19 | GroupAdmin | `POST /api/FeeHead` | name, accountCode | | feeHeadId | Step 19 |
| 20 | GroupAdmin | `POST /api/FeeStructure` | classId, feeHeadId, amount | 11, 19 | | Step 19 |
| 21 | GroupAdmin | `POST /api/ClassFeeMap` | classId, academicYearId, feeHeadId, amount, installmentNo | 8, 11, 19 | | Step 19 |
| 22 | GroupAdmin | `POST /api/FeeInstallment` | branchId, classId, academicYearId, installmentNo, dueDate | 7, 8, 11 | | Step 19 |
| 23 | GroupAdmin | `GET /api/ExamType` | | | examTypeId | Step 20 |
| 24 | GroupAdmin | `GET /api/GradeMaster` | | | | Step 20 |
| 25 | GroupAdmin | `POST /api/Exam` | name, examTypeId, academicYearId, branchId, classId, startDate, endDate | 7, 8, 11, 23 | examId | Step 20 |
| 26 | GroupAdmin | `POST /api/ExamSubject` | examId, subjectId, totalMaxMarks, totalPassMarks, examDate… | 13, 25 | | Step 20 |
| 27 | GroupAdmin | `POST /api/Timetable` | branchId, classId, divisionId, subjectId, teacherId, dayOfWeek, periodNo, academicYearId | 7–13 | | Step 21 |
| 28 | Principal | `POST /api/Student` | branchId, academicYearId, classId, divisionId, firstName, dob… | 7, 8, 11, 12 | studentId | Step 12 |
| 29 | Principal | `POST /api/ParentUser` | studentId, username, phone | 9, 28 | | Step 14 |

After call 29 the school is fully configured. Everything after that in this guide (lifecycle, permissions, bulk upload, isolation) is testing.

> `teacherId` in `/api/Timetable` is the **staffId** of the teacher.
> Every id is a GUID. In `.http` files, copy each id into the matching `@variable` at the top of the file.

---

## Part A: One-time setup (Visual Studio)

### Prerequisites

- Visual Studio 2022 (17.12 or newer, so the `.http` file can reuse tokens) with **ASP.NET and web development**.
- .NET 8 runtime (`dotnet --list-runtimes` shows `Microsoft.AspNetCore.App 8.0.x`).
- Docker Desktop, **running**.

### A1. Start PostgreSQL

1. Copy `.env.example` to `.env` in the repo root and set these values:
   ```
   POSTGRES_PASSWORD=Postgres@123
   JWT_KEY=local-dev-jwt-key-change-me-0123456789abcdef
   PLATFORM_ADMIN_PASSWORD=Admin@123
   ```
2. **View → Terminal**:
   ```powershell
   docker compose up -d db
   docker compose ps                 # db = healthy
   ```

### A2. Open and configure the API

1. Open `HCB_API.sln`.
2. Right-click **API** → **Set as Startup Project**.
3. Select the **http** profile in the run dropdown.
4. Right-click **API** → **Manage User Secrets**, add these values and save:
   ```json
   {
     "ConnectionStrings": {
       "PostgresConnection": "Host=localhost;Port=5433;Database=postgres;Username=postgres;Password=Postgres@123"
     },
     "Jwt": { "Key": "local-dev-jwt-key-change-me-0123456789abcdef" }
   }
   ```

### A3. Run

Press **F5**. Wait for `Now listening on: http://localhost:5102` in the Output window. Swagger opens at **http://localhost:5102/swagger**.

### A4. See the databases

```powershell
docker compose exec db psql -U postgres -c "\l"
```

After the first start you should see **`bonzer_catalog`** (common) and **`bonzer_school_demo`** (the demo school group that Development seeds for you).
Leave the demo alone; below you build your own school group from scratch.

> To browse tables, run `docker compose --profile tools up -d`, open pgAdmin at http://localhost:5050 (`admin@bonzer.local` / `admin`), and add server `db`, port `5432`, user `postgres`.

---

## Part B: Platform level (Super Admin, common database)

### Step 1: Log in as Super Admin

`POST /api/Auth/login`
```json
{ "email": "superadmin@bonzer.local", "password": "Admin@123" }
```
Expected result: **200** with `accessToken`. In Swagger, click **Authorize** and paste the token only (no `Bearer ` prefix).

The Super Admin is created automatically at startup from `PlatformAdmin` settings. It has **no tenant**.

### Step 2: Check the shared lookups (in `bonzer_catalog`)

| Request | Expected result |
|---|---|
| `GET /api/InstitutionType` | Pre-Primary, School, Junior College. Note the **School** `id`. |
| `GET /api/Medium` | English, Semi-English, Marathi, Hindi. Note the **English** `id`. |
| `GET /api/SchoolGroup` | The `demo` school group, with `databaseName: bonzer_school_demo` |

### Step 3: Create the school group (= tenant), which creates its database

`POST /api/SchoolGroup` (Super Admin only)
```json
{
  "identifier": "sunrise-trust",
  "name": "Sunrise Education Trust",
  "code": "SET",
  "address": "FC Road, Pune",
  "phone": "9800000001",
  "email": "office@sunrise.test",
  "adminEmail": "admin@sunrise.test",
  "adminPassword": "Sunrise@123",
  "adminFirstName": "Group",
  "adminLastName": "Admin"
}
```

Expected result: **201** with `databaseName: "bonzer_school_sunrise_trust"` and `isActive: true`.

Verify:

```powershell
docker compose exec db psql -U postgres -c "\l"                                                   # new database listed
docker compose exec db psql -U postgres -d bonzer_catalog -c "select id, identifier, name, database_name, is_active from organizations;"
```

The `organizations.id` is the school group id and the `tenant_id` of every login of that group; branches store it as `group_id`.

Negative tests (each should return **400**):

| Change | Reason |
|---|---|
| Same `identifier` again | School group already exists |
| `"identifier": "Sunrise Trust"` (spaces/capitals) or `"ab"` | Identifier must be 3–40 characters: lower-case letters, digits and hyphens, starting with a letter |
| `adminEmail` already used by any login | Email already registered (emails are unique across **all** tenants, because logins live in the common DB) |
| `"adminPassword": "123"` | Password policy |

---

## Part C: School group level (Group Admin, tenant database)

### Step 4: Log in as the new Group Admin

`POST /api/Auth/login` `{ "email": "admin@sunrise.test", "password": "Sunrise@123" }` → Authorize with the new token.

From now on every request goes to `bonzer_school_sunrise_trust` automatically, because of the token.

| Request | Expected result |
|---|---|
| `GET /api/SchoolGroup` | Only your own group: Sunrise Education Trust |
| `PUT /api/SchoolGroup/{id}` with a changed name or phone | **200** |
| `PUT /api/SchoolGroup/{another group's id}` | **404** (a group admin cannot see other groups) |
| `POST /api/SchoolGroup` | **403**: only the Super Admin creates school groups |
| `GET /api/Branch` | Empty list (the new school has nothing yet) |
| `GET /api/Department` | Seeded departments (Administration, Teaching…) |
| `GET /api/ExamType`, `GET /api/FeeHead` | Seeded defaults |

### Step 5: Create a branch (school / campus)

`POST /api/Branch`
```json
{
  "institutionTypeId": "<School id from Step 2>",
  "mediumId": "<English id from Step 2>",
  "schooluid": "27250100101",
  "name": "Sunrise English School - Kothrud",
  "address": "Karve Road",
  "city": "Pune",
  "state": "Maharashtra",
  "phone": "9800000002",
  "email": "kothrud@sunrise.test"
}
```
Expected result: **201**. Note the **branchId**. You don't need `groupId`; it is filled with your school group's id. One school group can have many branches.
Create a second branch (e.g. "Sunrise English School - Baner") for the transfer test later.

The request is rejected if `name` is missing (**400**), or if `groupId` is set to another tenant's id.

### Step 6: Create the academic year

`POST /api/AcademicYear`
```json
{ "yearName": "2026-27", "startDate": "2026-06-01", "endDate": "2027-04-30", "isActive": true }
```
Expected result: **201**. Note the **academicYearId**. An `endDate` before `startDate` returns **400**.

### Step 7: Create user logins and assign roles

Roles already exist; you only assign them. List them with `GET /api/Role`.

`POST /api/User/register` (one call per person):

| firstName | lastName | email | password | role |
|---|---|---|---|---|
| Neha | Kulkarni | `principal@sunrise.test` | `Sunrise@123` | `Principal` |
| Amit | Joshi | `branchadmin@sunrise.test` | `Sunrise@123` | `BranchAdmin` |
| Ritu | Desai | `classteacher@sunrise.test` | `Sunrise@123` | `ClassTeacher` |
| Sameer | Khan | `teacher@sunrise.test` | `Sunrise@123` | `Teacher` |
| Pooja | More | `accountant@sunrise.test` | `Sunrise@123` | `Accountant` |
| Rahul | Patil | `parent@sunrise.test` | `Sunrise@123` | `Parent` |

Example body:
```json
{ "firstName": "Neha", "lastName": "Kulkarni", "email": "principal@sunrise.test", "password": "Sunrise@123", "role": "Principal" }
```

Verify:

| Test | Expected result |
|---|---|
| `GET /api/User` | Only Sunrise logins, never the demo tenant's |
| `POST /api/User/add-role` / `remove-role` | Adds / removes a role |
| Register with `"role": "Wizard"` | **400** role does not exist |
| As **BranchAdmin**, register someone with `"role": "GroupAdmin"` | Refused: only a Group Admin can grant Group Admin |
| `select email, tenant_id from "AspNetUsers";` in `bonzer_catalog` | New logins carry the Sunrise tenant id |

---

## Part D: Branch level (staff, classes, divisions, subjects)

You can stay logged in as Group Admin, or log in as `principal@sunrise.test`.

### Step 8: Staff records (linked to the logins)

A login becomes "a staff member" through a **staff record with the same email**.

`POST /api/Staff`, once for each staff login from Step 7:
```json
{
  "branchId": "<branchId>",
  "deptId": "<Teaching dept id from GET /api/Department>",
  "staffCode": "SET-T-001",
  "designation": "Class Teacher",
  "firstName": "Ritu",
  "lastName": "Desai",
  "gender": "Female",
  "phone": "9800000010",
  "email": "classteacher@sunrise.test",
  "joiningDate": "2026-06-01",
  "isActive": true
}
```
Note the class teacher's **staffId**.

### Step 9: Classes

`POST /api/Class` → `{ "branchId": "<branchId>", "name": "1st" }`
Create "1st", "2nd" and "3rd". Note **classId** (1st) and **higherClassId** (2nd).
Missing `branchId` returns **400**. As **teacher@sunrise.test** the call returns **403**.

### Step 10: Divisions

`POST /api/Division`
```json
{ "classId": "<classId>", "branchId": "<branchId>", "name": "A", "code": "1A", "availableSeats": 40 }
```
Create 1-A and 1-B. Note **divisionId** (1-A). `availableSeats: 5000` returns **400** (allowed range 0–1000).

### Step 11: Subjects and mapping

1. `POST /api/Subject` → `{ "name": "English", "code": "ENG" }`. Also add Mathematics `MAT` and Science `SCI`.
2. `POST /api/ClassSubjectMap` → `{ "classId": "<classId>", "divisionId": "<divisionId>", "subjectId": "<English id>", "academicYear": "2026-27" }`
3. `POST /api/ClassTeacherMap` → `{ "classId": "<classId>", "divisionId": "<divisionId>", "staffId": "<class teacher staffId>", "academicYear": "2026-27" }`

Check: `GET /api/ClassSubjectMap` and `GET /api/ClassTeacherMap` list the mappings.

### Step 11b: Subject teacher mapping

`POST /api/TeacherSubjectMap`
```json
{ "staffId": "<teacher staffId>", "subjectId": "<English id>", "classId": "<classId>", "divisionId": "<divisionId>", "academicYear": "2026-27" }
```
Create a staff record (Step 8) for `teacher@sunrise.test` first, to get this staffId.

---

## Part D2: Remaining school configuration (before or after admitting students)

Log in as Group Admin (or Principal). Fee endpoints need a finance role (GroupAdmin has it); exam, timetable and holiday endpoints need an academic or school leadership role.

### Step 18: Attendance calendar (weekly off + holidays)

`PUT /api/AttendanceHoliday/branch/{branchId}/weekly-off`
```json
{ "saturdayPattern": "secondFourth", "sundayOff": true }
```
`saturdayPattern` is one of `none`, `allSaturdays`, `secondFourth`, `firstThirdFifth`.

`POST /api/AttendanceHoliday/branch/{branchId}/holidays`
```json
{ "holidayDate": "2026-08-15", "name": "Independence Day" }
```
Check: `GET /api/AttendanceHoliday/branch/{branchId}/holidays?from=2026-06-01&to=2027-04-30` lists it. Remove with `DELETE /api/AttendanceHoliday/{holidayId}`.

### Step 19: Fees

1. **Fee head.** `GET /api/FeeHead` shows the seeded heads (e.g. Tuition). To add one:
   `POST /api/FeeHead` → `{ "name": "Tuition Fee", "accountCode": "TUI" }`. Note **feeHeadId**.
2. **Fee structure** (the base amount per class):
   `POST /api/FeeStructure` → `{ "classId": "<classId>", "feeHeadId": "<feeHeadId>", "amount": 24000 }`
3. **Class fee map** (the amount per academic year and installment):
   `POST /api/ClassFeeMap`
   ```json
   { "classId": "<classId>", "academicYearId": "<academicYearId>", "feeHeadId": "<feeHeadId>", "amount": 12000, "installmentNo": 1, "academicYear": "2026-27" }
   ```
   Repeat with `"installmentNo": 2`.
4. **Installment schedule:**
   `POST /api/FeeInstallment`
   ```json
   { "branchId": "<branchId>", "classId": "<classId>", "academicYearId": "<academicYearId>", "installmentNo": 1, "installmentName": "Term 1", "dueDate": "2026-07-10", "lateFeeAfter": "2026-07-25" }
   ```
   Repeat with installment 2 (`"dueDate": "2026-12-10"`).

Validation (each returns **400**): `amount` of 0 or less, missing `classId` / `feeHeadId` / `academicYearId`, `installmentNo` outside 1–24, `lateFeeAfter` before `dueDate`. A teacher login returns **403**.

### Step 20: Exams

1. `GET /api/ExamType` (seeded types, e.g. Unit Test, Term). Note **examTypeId**. Add one with `POST /api/ExamType` → `{ "name": "Unit Test 1", "description": "First unit test" }`.
2. `GET /api/GradeMaster` shows the seeded grade scale. Add one with `POST /api/GradeMaster` → `{ "grade": "A1", "minPercent": 91, "maxPercent": 100 }` only if you need a custom scale.
3. **Create the exam:** `POST /api/Exam`
   ```json
   {
     "name": "Unit Test 1 - 1st",
     "examTypeId": "<examTypeId>",
     "academicYearId": "<academicYearId>",
     "branchId": "<branchId>",
     "classId": "<classId>",
     "divisionId": "<divisionId>",
     "startDate": "2026-09-01",
     "endDate": "2026-09-05"
   }
   ```
   Note **examId**.
4. **Add each subject to the exam:** `POST /api/ExamSubject`
   ```json
   { "examId": "<examId>", "subjectId": "<English id>", "totalMaxMarks": 50, "totalPassMarks": 18, "examDate": "2026-09-01", "startTime": "09:00:00", "endTime": "10:30:00", "durationMinutes": 90 }
   ```
   Repeat for Mathematics and Science.

Check: `GET /api/Exam` and `GET /api/ExamSubject` list them.

### Step 21: Timetable

`POST /api/Timetable`
```json
{
  "branchId": "<branchId>",
  "classId": "<classId>",
  "divisionId": "<divisionId>",
  "subjectId": "<English id>",
  "teacherId": "<teacher staffId>",
  "dayOfWeek": 1,
  "periodNo": 1,
  "academicYearId": "<academicYearId>"
}
```
Use `dayOfWeek` 1 = Monday … 6 = Saturday. Repeat for each period. Check with `GET /api/Timetable`.

---

## Part E: Students (admission)

Log in as `principal@sunrise.test` (or Group Admin).

### Step 12: Admit a student

`POST /api/Student`
```json
{
  "branchId": "<branchId>",
  "academicYearId": "<academicYearId>",
  "classId": "<classId>",
  "divisionId": "<divisionId>",
  "firstName": "Aarav",
  "lastName": "Patil",
  "gender": "Male",
  "dob": "2020-05-10",
  "admissionDate": "2026-06-01",
  "fatherName": "Rahul Patil",
  "fatherPhone": "9800000020",
  "city": "Pune",
  "state": "Maharashtra"
}
```
Expected result: **201**. Note the **studentId**. Admit 2–3 more students.

Read checks: `GET /api/Student/paged`, `GET /api/Student/{id}`, `GET /api/Student/division/{divisionId}`,
`GET /api/Student/class-teacher/{staffId}` (students of the class teacher's division).

### Step 13: Validation (each returns **400**)

Use the Step 12 body and change one field at a time:

| Change | Rule |
|---|---|
| Remove `branchId` / `firstName` | Required |
| `"dob": "2030-01-01"` | Date of birth must be in the past |
| `"dob": "2025-01-01"` with admission 2026-06-01 | At least 2 years old at admission |
| `"admissionDate": "2028-12-31"` | Not more than a year ahead |
| `"fatherPhone": "12ab"`, `"email": "x"` | Invalid phone / email |
| `"childAadhaar": "123456789012"` | Invalid Aadhaar |
| `"studentStatus": "GRADUATED"` | Must be ACTIVE, SUSPENDED, DROPPED, TRANSFERRED, PASSED_OUT |

### Step 14: Link the parent

`POST /api/ParentUser` → `{ "studentId": "<studentId>", "username": "parent@sunrise.test", "phone": "9800000020" }`
Then log in as `parent@sunrise.test` and call `GET /api/Student/{studentId}`.

### Step 15: Student lifecycle

```
ACTIVE ──suspend──► SUSPENDED ──activate──► ACTIVE
ACTIVE / SUSPENDED ──drop──► DROPPED
ACTIVE ──passed-out──► PASSED_OUT
ACTIVE / SUSPENDED / PASSED_OUT / DROPPED ──issue-tc──► TRANSFERRED
DROPPED / TRANSFERRED ──re-enroll──► ACTIVE
ACTIVE ──promote / transfer──► ACTIVE (class or branch changes)
```
A disallowed step returns **409** with the reason.

| # | Request | Expected status |
|---|---|---|
| 1 | `POST /api/Student/{id}/suspend` | SUSPENDED |
| 2 | `…/suspend` again | **409** |
| 3 | `…/activate` | ACTIVE |
| 4 | `…/promote?newClassId=<higherClassId>&academicYearId=<academicYearId>` | ACTIVE, class = 2nd |
| 5 | `…/transfer?newBranchId=<second branch id>` | ACTIVE, branch = Baner |
| 6 | `…/drop` | DROPPED, leaving date set |
| 7 | `…/activate` | **409** (activate only from SUSPENDED) |
| 8 | `…/re-enroll` | ACTIVE |
| 9 | `…/issue-tc` | TRANSFERRED, `isTcIssued = true` |
| 10 | `…/issue-tc` again | **409** |
| 11 | `DELETE …/soft` then `POST …/restore` | Hidden from list, then back |

### Step 16: Permissions

| Log in as | Request | Expected result |
|---|---|---|
| `teacher@sunrise.test` | `GET /api/Student/paged` | **200** |
| `teacher@sunrise.test` | `POST /api/Student` | **403** |
| `accountant@sunrise.test` | `POST /api/Student/{id}/suspend` | **403** |
| `classteacher@sunrise.test` | `POST /api/Student` | **201** |
| `parent@sunrise.test` | `PUT /api/Student/{id}` | **403** |

### Step 17: Bulk upload and analytics

1. `GET /api/Student/bulk-upload/template`. Fill 3 rows plus 1 bad row, then upload it with `POST /api/Student/bulk-upload`. The good rows are imported and the bad row is reported.
2. Each of these should return **200**: `GET /api/Student/analytics/strength-by-class`, `…/gender-distribution`, `GET /api/Student/reports/udise`.

---

## Part F: Tenant isolation (most important multi-tenant test)

| # | Test | Expected result |
|---|---|---|
| F1 | Log in as `groupadmin@bonzer.local` / `Admin@123` (demo tenant). `GET /api/Student/paged` | Only the 10 **demo** students, none of Sunrise |
| F2 | Same user: `GET /api/Student/{Sunrise studentId}` | **404** (the id does not exist in the demo database) |
| F3 | Same user: send header `X-Tenant: sunrise-trust` | Ignored; still demo data. Tenant users cannot switch. |
| F4 | Super Admin with header `X-Tenant: sunrise-trust`: `GET /api/Branch` | Sunrise branches (support access) |
| F5 | Super Admin: `PUT /api/SchoolGroup/{sunrise id}/status` `{ "isActive": false }`, then log in as `admin@sunrise.test` | Login / calls refused; data kept |
| F6 | Set it back to `{ "isActive": true }` | Sunrise works again |

---

## Part G: Unit tests (Visual Studio Test Explorer)

**Test → Test Explorer** → **Run All**. To run one area, type `Organization`, `Auth` or `Students` in the search box, then click **Run**.
No database is needed (the tests use an in-memory database).

---

## Troubleshooting

| Symptom | Fix |
|---|---|
| `Failed to connect to localhost:5433` | Start Docker Desktop and run `docker compose up -d db`. |
| `password authentication failed` | The secrets.json password differs from the `.env` password. If you changed `.env` after the first start: `docker compose down -v` (deletes all data), then `up -d db`. |
| `Database 'bonzer_school_x' already exists` on create tenant | Choose another identifier, or drop the leftover database. |
| `Select a school group (tenant)…` | You called a school endpoint as Super Admin without `X-Tenant`. |
| Every call returns 401 after login | Paste the token without `Bearer `, or log in again (the token expired). |
| 403 | Expected for that role; see "Who may do what" in Part 0. |
| Demo logins missing | Run with the **http** profile (Development). |

---

## Result log

| Step | Pass / Fail | Notes |
|---|---|---|
| A4 Catalog + demo DB exist | | |
| 1–2 Super Admin + lookups | | |
| 3 School group + database created | | |
| 4 Group Admin, school group | | |
| 5 Branch | | |
| 6 Academic year | | |
| 7 Users + roles | | |
| 8 Staff | | |
| 9–10 Class, division | | |
| 11 Subjects, mapping | | |
| 11b Teacher-subject mapping | | |
| 18 Weekly off + holidays | | |
| 19 Fees (head, structure, class map, installments) | | |
| 20 Exams (exam + exam subjects) | | |
| 21 Timetable | | |
| 12–13 Student create + validation | | |
| 14 Parent link | | |
| 15 Lifecycle | | |
| 16 Permissions | | |
| 17 Bulk upload, analytics | | |
| F Tenant isolation | | |
| G Unit tests | | |
