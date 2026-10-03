-- =============================================================================
-- BonzerSchool - scenario seed: admission + attendance modules
-- - ~200 students (10 per division x 20 divisions across 2 branches / 4 classes)
-- - Weekday attendance for 3 calendar months (Dec 2025 - Feb 2026)
-- - Samples: leave_requests, attendance_correction_requests, date/year locks, parent_users
--
-- Safe to re-run: removes rows tagged with Schooluid LIKE 'SCENARIO-SEED%' on branches
-- and related keys listed in the cleanup section.
--
-- Requires: extension "uuid-ossp" (matches BonzerSchoolDbContext)
-- =============================================================================

BEGIN;

-- ---------------------------------------------------------------------------
-- Cleanup (FK-safe order)
-- ---------------------------------------------------------------------------
DELETE FROM attendance_correction_requests acr
USING attendance a
WHERE acr."AttendanceId" = a."Id"
  AND a."StudentId" IN (
    SELECT s."Id" FROM students s
    JOIN branches b ON s."BranchId" = b."Id"
    WHERE b."Schooluid" LIKE 'SCENARIO-SEED%'
  );

DELETE FROM attendance a
WHERE a."StudentId" IN (
  SELECT s."Id" FROM students s
  JOIN branches b ON s."BranchId" = b."Id"
  WHERE b."Schooluid" LIKE 'SCENARIO-SEED%'
);

DELETE FROM leave_requests lr
WHERE lr."StudentId" IN (
  SELECT s."Id" FROM students s
  JOIN branches b ON s."BranchId" = b."Id"
  WHERE b."Schooluid" LIKE 'SCENARIO-SEED%'
);

DELETE FROM attendance_date_locks d
WHERE d."DivisionId" IN (
  SELECT d0."Id" FROM divisions d0
  JOIN branches b ON d0."BranchId" = b."Id"
  WHERE b."Schooluid" LIKE 'SCENARIO-SEED%'
);

DELETE FROM attendance_academic_year_locks yl
WHERE yl."AcademicYearId" = '10000004-0000-4000-8000-000000000001'::uuid;

DELETE FROM parent_users pu
WHERE pu."StudentId" IN (
  SELECT s."Id" FROM students s
  JOIN branches b ON s."BranchId" = b."Id"
  WHERE b."Schooluid" LIKE 'SCENARIO-SEED%'
);

DELETE FROM students s
USING branches b
WHERE s."BranchId" = b."Id" AND b."Schooluid" LIKE 'SCENARIO-SEED%';

DELETE FROM divisions d0
WHERE d0."BranchId" IN (SELECT "Id" FROM branches WHERE "Schooluid" LIKE 'SCENARIO-SEED%');

DELETE FROM classes c
WHERE c."BranchId" IN (SELECT "Id" FROM branches WHERE "Schooluid" LIKE 'SCENARIO-SEED%');

DELETE FROM branches WHERE "Schooluid" LIKE 'SCENARIO-SEED%';

DELETE FROM academic_years WHERE "Id" = '10000004-0000-4000-8000-000000000001'::uuid;

DELETE FROM school_groups WHERE "Code" = 'SCENARIO-GROUP';
DELETE FROM institution_types WHERE "Id" = '10000002-0000-4000-8000-000000000001'::uuid;
DELETE FROM mediums WHERE "Id" = '10000003-0000-4000-8000-000000000001'::uuid;

-- ---------------------------------------------------------------------------
-- Lookup rows (branch FKs)
-- ---------------------------------------------------------------------------
INSERT INTO school_groups ("Id", "Name", "Code", "Address", "Phone", "Email", "CreatedAt")
VALUES (
  '10000001-0000-4000-8000-000000000001'::uuid,
  'Scenario Apex School Group',
  'SCENARIO-GROUP',
  '100 Seed Avenue',
  '9000000000',
  'scenario.group@example.com',
  CURRENT_TIMESTAMP
);

INSERT INTO institution_types ("Id", "Name")
VALUES ('10000002-0000-4000-8000-000000000001'::uuid, 'Scenario Institution Type');

INSERT INTO mediums ("Id", "Name")
VALUES ('10000003-0000-4000-8000-000000000001'::uuid, 'Scenario Medium');

INSERT INTO academic_years ("Id", "YearName", "StartDate", "EndDate", "IsActive")
VALUES (
  '10000004-0000-4000-8000-000000000001'::uuid,
  '2025-26 Scenario',
  DATE '2025-04-01',
  DATE '2026-03-31',
  TRUE
);

INSERT INTO branches (
  "Id", "GroupId", "InstitutionTypeId", "MediumId", "Schooluid", "Name", "Address", "City", "State", "Phone", "Email", "CreatedAt"
)
VALUES
(
  '10000005-0000-4000-8000-000000000001'::uuid,
  '10000001-0000-4000-8000-000000000001'::uuid,
  '10000002-0000-4000-8000-000000000001'::uuid,
  '10000003-0000-4000-8000-000000000001'::uuid,
  'SCENARIO-SEED-N',
  'Scenario School - North Campus',
  '1 Seed Street, Block A',
  'Mumbai',
  'MH',
  '9000000001',
  'scenario.north@example.com',
  CURRENT_TIMESTAMP
),
(
  '10000006-0000-4000-8000-000000000001'::uuid,
  '10000001-0000-4000-8000-000000000001'::uuid,
  '10000002-0000-4000-8000-000000000001'::uuid,
  '10000003-0000-4000-8000-000000000001'::uuid,
  'SCENARIO-SEED-S',
  'Scenario School - South Campus',
  '2 Seed Street, Block B',
  'Bengaluru',
  'KA',
  '9000000002',
  'scenario.south@example.com',
  CURRENT_TIMESTAMP
);

INSERT INTO classes ("Id", "BranchId", "Name")
VALUES
('10000007-0000-4000-8000-000000000001'::uuid, '10000005-0000-4000-8000-000000000001'::uuid, 'Grade 1'),
('10000008-0000-4000-8000-000000000001'::uuid, '10000005-0000-4000-8000-000000000001'::uuid, 'Grade 2'),
('10000009-0000-4000-8000-000000000001'::uuid, '10000006-0000-4000-8000-000000000001'::uuid, 'Grade 1'),
('10000010-0000-4000-8000-000000000001'::uuid, '10000006-0000-4000-8000-000000000001'::uuid, 'Grade 2');

-- 20 divisions (5 per class), available seats for admission analytics
INSERT INTO divisions ("Id", "ClassId", "BranchId", "Name", "Code", "AvailableSeats", "CreatedAt")
VALUES
('20000000-0000-4000-8000-000000000001'::uuid, '10000007-0000-4000-8000-000000000001'::uuid, '10000005-0000-4000-8000-000000000001'::uuid, 'A', 'N-G1-A', 40, CURRENT_TIMESTAMP),
('20000000-0000-4000-8000-000000000002'::uuid, '10000007-0000-4000-8000-000000000001'::uuid, '10000005-0000-4000-8000-000000000001'::uuid, 'B', 'N-G1-B', 40, CURRENT_TIMESTAMP),
('20000000-0000-4000-8000-000000000003'::uuid, '10000007-0000-4000-8000-000000000001'::uuid, '10000005-0000-4000-8000-000000000001'::uuid, 'C', 'N-G1-C', 40, CURRENT_TIMESTAMP),
('20000000-0000-4000-8000-000000000004'::uuid, '10000007-0000-4000-8000-000000000001'::uuid, '10000005-0000-4000-8000-000000000001'::uuid, 'D', 'N-G1-D', 40, CURRENT_TIMESTAMP),
('20000000-0000-4000-8000-000000000005'::uuid, '10000007-0000-4000-8000-000000000001'::uuid, '10000005-0000-4000-8000-000000000001'::uuid, 'E', 'N-G1-E', 35, CURRENT_TIMESTAMP),
('20000000-0000-4000-8000-000000000006'::uuid, '10000008-0000-4000-8000-000000000001'::uuid, '10000005-0000-4000-8000-000000000001'::uuid, 'A', 'N-G2-A', 40, CURRENT_TIMESTAMP),
('20000000-0000-4000-8000-000000000007'::uuid, '10000008-0000-4000-8000-000000000001'::uuid, '10000005-0000-4000-8000-000000000001'::uuid, 'B', 'N-G2-B', 40, CURRENT_TIMESTAMP),
('20000000-0000-4000-8000-000000000008'::uuid, '10000008-0000-4000-8000-000000000001'::uuid, '10000005-0000-4000-8000-000000000001'::uuid, 'C', 'N-G2-C', 40, CURRENT_TIMESTAMP),
('20000000-0000-4000-8000-000000000009'::uuid, '10000008-0000-4000-8000-000000000001'::uuid, '10000005-0000-4000-8000-000000000001'::uuid, 'D', 'N-G2-D', 40, CURRENT_TIMESTAMP),
('20000000-0000-4000-8000-00000000000a'::uuid, '10000008-0000-4000-8000-000000000001'::uuid, '10000005-0000-4000-8000-000000000001'::uuid, 'E', 'N-G2-E', 35, CURRENT_TIMESTAMP),
('20000000-0000-4000-8000-000000000011'::uuid, '10000009-0000-4000-8000-000000000001'::uuid, '10000006-0000-4000-8000-000000000001'::uuid, 'A', 'S-G1-A', 40, CURRENT_TIMESTAMP),
('20000000-0000-4000-8000-000000000012'::uuid, '10000009-0000-4000-8000-000000000001'::uuid, '10000006-0000-4000-8000-000000000001'::uuid, 'B', 'S-G1-B', 40, CURRENT_TIMESTAMP),
('20000000-0000-4000-8000-000000000013'::uuid, '10000009-0000-4000-8000-000000000001'::uuid, '10000006-0000-4000-8000-000000000001'::uuid, 'C', 'S-G1-C', 40, CURRENT_TIMESTAMP),
('20000000-0000-4000-8000-000000000014'::uuid, '10000009-0000-4000-8000-000000000001'::uuid, '10000006-0000-4000-8000-000000000001'::uuid, 'D', 'S-G1-D', 40, CURRENT_TIMESTAMP),
('20000000-0000-4000-8000-000000000015'::uuid, '10000009-0000-4000-8000-000000000001'::uuid, '10000006-0000-4000-8000-000000000001'::uuid, 'E', 'S-G1-E', 35, CURRENT_TIMESTAMP),
('20000000-0000-4000-8000-000000000016'::uuid, '10000010-0000-4000-8000-000000000001'::uuid, '10000006-0000-4000-8000-000000000001'::uuid, 'A', 'S-G2-A', 40, CURRENT_TIMESTAMP),
('20000000-0000-4000-8000-000000000017'::uuid, '10000010-0000-4000-8000-000000000001'::uuid, '10000006-0000-4000-8000-000000000001'::uuid, 'B', 'S-G2-B', 40, CURRENT_TIMESTAMP),
('20000000-0000-4000-8000-000000000018'::uuid, '10000010-0000-4000-8000-000000000001'::uuid, '10000006-0000-4000-8000-000000000001'::uuid, 'C', 'S-G2-C', 40, CURRENT_TIMESTAMP),
('20000000-0000-4000-8000-000000000019'::uuid, '10000010-0000-4000-8000-000000000001'::uuid, '10000006-0000-4000-8000-000000000001'::uuid, 'D', 'S-G2-D', 40, CURRENT_TIMESTAMP),
('20000000-0000-4000-8000-00000000001a'::uuid, '10000010-0000-4000-8000-000000000001'::uuid, '10000006-0000-4000-8000-000000000001'::uuid, 'E', 'S-G2-E', 35, CURRENT_TIMESTAMP);

-- ---------------------------------------------------------------------------
-- 200 students
-- ---------------------------------------------------------------------------
WITH div_map AS (
  SELECT * FROM (VALUES
    (1, '10000005-0000-4000-8000-000000000001'::uuid, '10000007-0000-4000-8000-000000000001'::uuid, '20000000-0000-4000-8000-000000000001'::uuid),
    (2, '10000005-0000-4000-8000-000000000001'::uuid, '10000007-0000-4000-8000-000000000001'::uuid, '20000000-0000-4000-8000-000000000002'::uuid),
    (3, '10000005-0000-4000-8000-000000000001'::uuid, '10000007-0000-4000-8000-000000000001'::uuid, '20000000-0000-4000-8000-000000000003'::uuid),
    (4, '10000005-0000-4000-8000-000000000001'::uuid, '10000007-0000-4000-8000-000000000001'::uuid, '20000000-0000-4000-8000-000000000004'::uuid),
    (5, '10000005-0000-4000-8000-000000000001'::uuid, '10000007-0000-4000-8000-000000000001'::uuid, '20000000-0000-4000-8000-000000000005'::uuid),
    (6, '10000005-0000-4000-8000-000000000001'::uuid, '10000008-0000-4000-8000-000000000001'::uuid, '20000000-0000-4000-8000-000000000006'::uuid),
    (7, '10000005-0000-4000-8000-000000000001'::uuid, '10000008-0000-4000-8000-000000000001'::uuid, '20000000-0000-4000-8000-000000000007'::uuid),
    (8, '10000005-0000-4000-8000-000000000001'::uuid, '10000008-0000-4000-8000-000000000001'::uuid, '20000000-0000-4000-8000-000000000008'::uuid),
    (9, '10000005-0000-4000-8000-000000000001'::uuid, '10000008-0000-4000-8000-000000000001'::uuid, '20000000-0000-4000-8000-000000000009'::uuid),
    (10, '10000005-0000-4000-8000-000000000001'::uuid, '10000008-0000-4000-8000-000000000001'::uuid, '20000000-0000-4000-8000-00000000000a'::uuid),
    (11, '10000006-0000-4000-8000-000000000001'::uuid, '10000009-0000-4000-8000-000000000001'::uuid, '20000000-0000-4000-8000-000000000011'::uuid),
    (12, '10000006-0000-4000-8000-000000000001'::uuid, '10000009-0000-4000-8000-000000000001'::uuid, '20000000-0000-4000-8000-000000000012'::uuid),
    (13, '10000006-0000-4000-8000-000000000001'::uuid, '10000009-0000-4000-8000-000000000001'::uuid, '20000000-0000-4000-8000-000000000013'::uuid),
    (14, '10000006-0000-4000-8000-000000000001'::uuid, '10000009-0000-4000-8000-000000000001'::uuid, '20000000-0000-4000-8000-000000000014'::uuid),
    (15, '10000006-0000-4000-8000-000000000001'::uuid, '10000009-0000-4000-8000-000000000001'::uuid, '20000000-0000-4000-8000-000000000015'::uuid),
    (16, '10000006-0000-4000-8000-000000000001'::uuid, '10000010-0000-4000-8000-000000000001'::uuid, '20000000-0000-4000-8000-000000000016'::uuid),
    (17, '10000006-0000-4000-8000-000000000001'::uuid, '10000010-0000-4000-8000-000000000001'::uuid, '20000000-0000-4000-8000-000000000017'::uuid),
    (18, '10000006-0000-4000-8000-000000000001'::uuid, '10000010-0000-4000-8000-000000000001'::uuid, '20000000-0000-4000-8000-000000000018'::uuid),
    (19, '10000006-0000-4000-8000-000000000001'::uuid, '10000010-0000-4000-8000-000000000001'::uuid, '20000000-0000-4000-8000-000000000019'::uuid),
    (20, '10000006-0000-4000-8000-000000000001'::uuid, '10000010-0000-4000-8000-000000000001'::uuid, '20000000-0000-4000-8000-00000000001a'::uuid)
  ) AS t(div_ix, branch_id, class_id, division_id)
),
grid AS (
  SELECT
    i,
    1 + (i - 1) / 10 AS div_ix
 FROM generate_series(1, 200) AS i
)
INSERT INTO students (
  "Id", "BranchId", "AcademicYearId", "AdmissionNo", "AdmissionDate", "FirstName", "LastName", "Gender", "Dob",
  "ClassId", "DivisionId", "IsActive", "CreatedAt", "Category", "Religion", "Caste",
  roll_number, student_status, ews_dg_category, is_rte_seat, rte_eligible, is_minority, is_bpl, single_girl_child
)
SELECT
  uuid_generate_v5('6ba7b811-9dad-11d1-80b4-00c04fd430c8'::uuid, 'bonzer-scenario-student-' || g.i::text),
  m.branch_id,
  '10000004-0000-4000-8000-000000000001'::uuid,
  'SCN-' || LPAD(g.i::text, 4, '0'),
  DATE '2025-05-15' + ((g.i % 45) * INTERVAL '1 day'),
  'Student',
  'Scenario' || g.i::text,
  CASE WHEN g.i % 2 = 0 THEN 'Female' ELSE 'Male' END,
  DATE '2014-06-01' + (((g.i * 17) % 800) * INTERVAL '1 day'),
  m.class_id,
  m.division_id,
  TRUE,
  CURRENT_TIMESTAMP,
  (ARRAY['General', 'OBC', 'SC', 'ST', 'EWS'])[1 + (g.i % 5)],
  (ARRAY['Hindu', 'Muslim', 'Christian', 'Sikh', 'Other'])[1 + (g.i % 5)],
  (ARRAY['OPEN', 'OBC', 'SC', 'ST', 'OTHER'])[1 + (g.i % 5)],
  LPAD(g.i::text, 4, '0'),
  'ACTIVE',
  CASE WHEN g.i % 9 = 0 THEN 'EWS' WHEN g.i % 11 = 0 THEN 'DG' ELSE NULL END,
  (g.i % 18 = 0),
  (g.i % 18 = 0),
  (g.i % 15 = 0),
  (g.i % 20 = 0),
  ((g.i % 2 = 0) AND (g.i % 7 = 0))
FROM grid g
JOIN div_map m ON m.div_ix = g.div_ix;

-- ---------------------------------------------------------------------------
-- Parent portal users (one per student; password placeholder â€” not for login)
-- ---------------------------------------------------------------------------
INSERT INTO parent_users ("Id", "StudentId", "Username", "PasswordHash", "Phone")
SELECT
  uuid_generate_v5('6ba7b811-9dad-11d1-80b4-00c04fd430c8'::uuid, 'bonzer-scenario-parent-' || g.i::text),
  uuid_generate_v5('6ba7b811-9dad-11d1-80b4-00c04fd430c8'::uuid, 'bonzer-scenario-student-' || g.i::text),
  'parent_scn_' || g.i::text,
  'SEED-NOT-A-REAL-HASH',
  '9876500' || LPAD((g.i % 10000)::text, 4, '0')
FROM generate_series(1, 200) AS g(i);

-- ---------------------------------------------------------------------------
-- Attendance: weekdays only, ~last 120 days through today (so Month/Week UIs show data)
-- Reproducible distribution: mostly Present; some Absent / Leave.
-- ---------------------------------------------------------------------------
SELECT setseed(0.42);

INSERT INTO attendance (
  "Id",
  "StudentId",
  "ClassId",
  "DivisionId",
  "AttendanceDate",
  "Status",
  "LeaveType",
  "IsHoliday",
  "IsDeleted",
  "CreatedAt"
)
SELECT
  uuid_generate_v4(),
  s."Id",
  s."ClassId",
  s."DivisionId",
  d.dt,
  CASE
    WHEN random() < 0.88 THEN 'Present'
    WHEN random() < 0.95 THEN 'Absent'
    ELSE 'Leave'
  END,
  CASE
    WHEN random() < 0.95 THEN NULL
    WHEN random() < 0.5 THEN 'Sick'
    ELSE 'Personal'
  END,
  FALSE,
  FALSE,
  CURRENT_TIMESTAMP
FROM students s
JOIN branches b ON s."BranchId" = b."Id" AND b."Schooluid" LIKE 'SCENARIO-SEED%'
CROSS JOIN LATERAL (
  SELECT dd::date AS dt
  FROM generate_series((CURRENT_DATE - INTERVAL '120 days')::date, CURRENT_DATE::date, INTERVAL '1 day') AS dd
  WHERE EXTRACT(ISODOW FROM dd::date) < 6
) d;

-- Align LeaveType with status (deterministic by student + date)
UPDATE attendance a
SET "LeaveType" = CASE (mod(abs(hashtext(a."StudentId"::text || a."AttendanceDate"::text)), 3))
    WHEN 0 THEN 'Sick'
    WHEN 1 THEN 'Personal'
    ELSE 'Emergency'
  END
FROM students s
JOIN branches b ON s."BranchId" = b."Id"
WHERE a."StudentId" = s."Id"
  AND b."Schooluid" LIKE 'SCENARIO-SEED%'
  AND a."Status" = 'Leave';

UPDATE attendance a
SET "LeaveType" = NULL
FROM students s
JOIN branches b ON s."BranchId" = b."Id"
WHERE a."StudentId" = s."Id"
  AND b."Schooluid" LIKE 'SCENARIO-SEED%'
  AND a."Status" <> 'Leave';

-- ---------------------------------------------------------------------------
-- Leave requests (40 rows; mix of Pending / Approved)
-- ---------------------------------------------------------------------------
INSERT INTO leave_requests (
  "Id", "StudentId", "FromDate", "ToDate", "LeaveType", "Reason", "Status", "RequestedAt"
)
SELECT
  uuid_generate_v4(),
  x.sid,
  DATE '2026-01-05' + (((x.rn * 2) % 20) * INTERVAL '1 day'),
  DATE '2026-01-05' + ((((x.rn * 2) % 20) + (x.rn % 2)) * INTERVAL '1 day'),
  CASE x.rn % 3 WHEN 0 THEN 'Sick' WHEN 1 THEN 'Personal' ELSE 'Family' END,
  'Scenario seed leave #' || x.rn::text,
  CASE WHEN x.rn % 5 = 0 THEN 'Pending' ELSE 'Approved' END,
  CURRENT_TIMESTAMP - ((x.rn % 15) * INTERVAL '1 day')
FROM (
  SELECT s."Id" AS sid, row_number() OVER (ORDER BY s."AdmissionNo") AS rn
  FROM students s
  JOIN branches b ON s."BranchId" = b."Id" AND b."Schooluid" LIKE 'SCENARIO-SEED%'
) x
WHERE x.rn <= 40;

-- ---------------------------------------------------------------------------
-- Attendance correction requests (sample linked to Absent rows)
-- ---------------------------------------------------------------------------
INSERT INTO attendance_correction_requests ("Id", "AttendanceId", "Reason", "Status", "RequestedAt")
SELECT
  uuid_generate_v4(),
  a."Id",
  'Scenario seed: mark should be Present',
  CASE WHEN row_number() OVER () % 4 = 0 THEN 'Pending' ELSE 'Approved' END,
  CURRENT_TIMESTAMP
FROM attendance a
JOIN students s ON a."StudentId" = s."Id"
JOIN branches b ON s."BranchId" = b."Id"
WHERE b."Schooluid" LIKE 'SCENARIO-SEED%'
  AND a."Status" = 'Absent'
LIMIT 25;

-- ---------------------------------------------------------------------------
-- Locks (samples)
-- ---------------------------------------------------------------------------
INSERT INTO attendance_date_locks ("Id", "ClassId", "DivisionId", "AttendanceDate", "IsLocked", "LockedAt")
VALUES
(
  uuid_generate_v4(),
  '10000007-0000-4000-8000-000000000001'::uuid,
  '20000000-0000-4000-8000-000000000001'::uuid,
  DATE '2026-01-15',
  TRUE,
  CURRENT_TIMESTAMP
);

INSERT INTO attendance_academic_year_locks ("Id", "AcademicYearId", "IsLocked", "LockedAt")
VALUES
(
  uuid_generate_v4(),
  '10000004-0000-4000-8000-000000000001'::uuid,
  FALSE,
  NULL
);

COMMIT;

-- Done. Quick checks (optional):
-- SELECT COUNT(*) FROM students s JOIN branches b ON s."BranchId"=b."Id" WHERE b."Schooluid" LIKE 'SCENARIO-SEED%';
-- SELECT COUNT(*) FROM attendance a JOIN students s ON a."StudentId"=s."Id" JOIN branches b ON s."BranchId"=b."Id" WHERE b."Schooluid" LIKE 'SCENARIO-SEED%';
