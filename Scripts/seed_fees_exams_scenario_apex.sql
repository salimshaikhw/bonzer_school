-- =============================================================================
-- BonzerSchool – fees, exams, and marks seed for "Scenario Apex School Group"
--
-- Targets branches where school_groups.Code = 'SCENARIO-GROUP' OR
-- school_groups.Name = 'Scenario Apex School Group'.
--
-- Fee dashboards (API) sum invoices with InvoiceDate inside the academic year;
-- use academic year row id 10000004-0000-4000-8000-000000000001 when present
-- (created by Scripts/seed_admission_attendance_scenario.sql), or adjust below.
--
-- Safe to re-run: removes prior seed rows by tags / deterministic names.
--
-- Requires: uuid-ossp
--
-- Example:
--   set PGPASSWORD=your_password
--   psql -h localhost -p 5432 -U postgres -d NewBonzerSchool -f Scripts/seed_fees_exams_scenario_apex.sql
-- =============================================================================

BEGIN;

-- ---------------------------------------------------------------------------
-- Cleanup (FK-safe order)
-- ---------------------------------------------------------------------------
DELETE FROM payment_fee_lines pfl
USING payments pay
WHERE pfl."PaymentId" = pay."Id"
  AND pay."TransactionNumber" LIKE 'SCENARIO-FEE-%';

DELETE FROM payments WHERE "TransactionNumber" LIKE 'SCENARIO-FEE-%';

DELETE FROM invoice_items ii
USING invoices inv
WHERE ii."InvoiceId" = inv."Id"
  AND inv."InvoiceNo" LIKE 'SCENARIO-FEE-%';

DELETE FROM invoices WHERE "InvoiceNo" LIKE 'SCENARIO-FEE-%';

DELETE FROM marks WHERE "Remark" = 'SCENARIO-MARK-SEED';

DELETE FROM exam_subject_components esc
USING exam_subjects es
JOIN exams ex ON es."ExamId" = ex."Id"
WHERE esc."ExamSubjectId" = es."Id"
  AND ex."Name" LIKE 'Scenario Exam %';

DELETE FROM exam_component_types
WHERE "Id" IN (
  '30000053-0000-4000-8000-000000000001'::uuid,
  '30000053-0000-4000-8000-000000000002'::uuid
);

DELETE FROM exam_subjects es
USING exams ex
WHERE es."ExamId" = ex."Id"
  AND ex."Name" LIKE 'Scenario Exam %';

DELETE FROM exams WHERE "Name" LIKE 'Scenario Exam %';

DELETE FROM exam_types WHERE "Id" IN (
  '30000050-0000-4000-8000-000000000001'::uuid,
  '30000050-0000-4000-8000-000000000002'::uuid
);

DELETE FROM subjects WHERE "Code" IN ('SCENARIO-MATH', 'SCENARIO-ENG', 'SCENARIO-SCI');

DELETE FROM class_fee_map
WHERE "FeeHeadId" IN (
  '30000001-0000-4000-8000-000000000001'::uuid,
  '30000001-0000-4000-8000-000000000002'::uuid,
  '30000001-0000-4000-8000-000000000003'::uuid
);

DELETE FROM fee_installments
WHERE "InstallmentName" IN ('SCENARIO-SEED-Q1', 'SCENARIO-SEED-Q2', 'SCENARIO-SEED-Q3');

DELETE FROM fee_heads
WHERE "Id" IN (
  '30000001-0000-4000-8000-000000000001'::uuid,
  '30000001-0000-4000-8000-000000000002'::uuid,
  '30000001-0000-4000-8000-000000000003'::uuid
);

-- ---------------------------------------------------------------------------
-- Master: fee heads
-- ---------------------------------------------------------------------------
INSERT INTO fee_heads ("Id", "Name", "AccountCode")
VALUES
  ('30000001-0000-4000-8000-000000000001'::uuid, 'Tuition', 'SCENARIO-TUITION'),
  ('30000001-0000-4000-8000-000000000002'::uuid, 'Transport', 'SCENARIO-TRANS'),
  ('30000001-0000-4000-8000-000000000003'::uuid, 'Laboratory', 'SCENARIO-LAB');

-- Installments: 3 per branch in Apex group (ids deterministic per branch)
INSERT INTO fee_installments (
  "Id", "BranchId", "ClassId", "AcademicYearId", "InstallmentNo", "InstallmentName",
  "DueDate", "LateFeeAfter", "CreatedAt"
)
SELECT
  uuid_generate_v5('6ba7b811-9dad-11d1-80b4-00c04fd430c8'::uuid, 'scenario-fi-' || b."Id"::text || '-' || q.n::text),
  b."Id",
  NULL,
  '10000004-0000-4000-8000-000000000001'::uuid,
  q.n,
  q.installment_name,
  q.due_d,
  q.late_d,
  CURRENT_TIMESTAMP
FROM branches b
JOIN school_groups sg ON b."GroupId" = sg."Id"
CROSS JOIN (
  VALUES
    (1, 'SCENARIO-SEED-Q1', DATE '2025-05-15', DATE '2025-05-25'),
    (2, 'SCENARIO-SEED-Q2', DATE '2025-08-15', DATE '2025-08-25'),
    (3, 'SCENARIO-SEED-Q3', DATE '2025-11-15', DATE '2025-11-25')
) AS q(n, installment_name, due_d, late_d)
WHERE (sg."Code" = 'SCENARIO-GROUP' OR sg."Name" = 'Scenario Apex School Group')
  AND EXISTS (
    SELECT 1 FROM academic_years ay WHERE ay."Id" = '10000004-0000-4000-8000-000000000001'::uuid
  );

-- Class fee map: academic year × each class under Apex × 3 heads
INSERT INTO class_fee_map (
  "Id", "ClassId", "DivisionId", "AcademicYearId", "FeeHeadId", "Amount", "InstallmentNo", "AcademicYear", "CreatedAt"
)
SELECT
  uuid_generate_v5('6ba7b811-9dad-11d1-80b4-00c04fd430c8'::uuid, 'scenario-cfm-' || c."Id"::text || '-' || fh.fh_id::text),
  c."Id",
  NULL,
  '10000004-0000-4000-8000-000000000001'::uuid,
  fh.fh_id,
  fh.amt,
  1,
  '2025-26 Scenario',
  CURRENT_TIMESTAMP
FROM classes c
JOIN branches b ON c."BranchId" = b."Id"
JOIN school_groups sg ON b."GroupId" = sg."Id"
CROSS JOIN (
  VALUES
    ('30000001-0000-4000-8000-000000000001'::uuid, 18000.00::numeric),
    ('30000001-0000-4000-8000-000000000002'::uuid, 7200.00::numeric),
    ('30000001-0000-4000-8000-000000000003'::uuid, 4800.00::numeric)
) AS fh(fh_id, amt)
WHERE (sg."Code" = 'SCENARIO-GROUP' OR sg."Name" = 'Scenario Apex School Group')
  AND EXISTS (
    SELECT 1 FROM academic_years ay WHERE ay."Id" = '10000004-0000-4000-8000-000000000001'::uuid
  );

-- ---------------------------------------------------------------------------
-- Invoices + line items (branch dashboard uses invoices in academic year window)
-- ---------------------------------------------------------------------------
WITH apex_students AS (
  SELECT
    s."Id" AS student_id,
    s."BranchId" AS branch_id,
    s."ClassId" AS class_id,
    s."DivisionId" AS division_id,
    row_number() OVER (ORDER BY s."AdmissionNo") AS rn
  FROM students s
  JOIN branches b ON s."BranchId" = b."Id"
  JOIN school_groups sg ON b."GroupId" = sg."Id"
  WHERE (sg."Code" = 'SCENARIO-GROUP' OR sg."Name" = 'Scenario Apex School Group')
),
picked AS (
  SELECT * FROM apex_students WHERE rn <= 80
),
first_inst AS (
  SELECT DISTINCT ON (p.branch_id)
    p.branch_id,
    fi."Id" AS installment_id
  FROM picked p
  JOIN fee_installments fi ON fi."BranchId" = p.branch_id
    AND fi."AcademicYearId" = '10000004-0000-4000-8000-000000000001'::uuid
    AND fi."InstallmentName" IN ('SCENARIO-SEED-Q1', 'SCENARIO-SEED-Q2', 'SCENARIO-SEED-Q3')
  ORDER BY p.branch_id, fi."InstallmentNo"
)
INSERT INTO invoices (
  "Id", "StudentId", "BranchId", "ClassId", "DivisionId", "InstallmentId",
  "InvoiceNo", "InvoiceDate", "TotalAmount", "PaidAmount", "RemainingAmount",
  "DueDate", "Status", "CreatedAt"
)
SELECT
  uuid_generate_v5('6ba7b811-9dad-11d1-80b4-00c04fd430c8'::uuid, 'scenario-fee-inv-' || p.student_id::text),
  p.student_id,
  p.branch_id,
  p.class_id,
  p.division_id,
  fi.installment_id,
  'SCENARIO-FEE-' || LPAD(p.rn::text, 4, '0'),
  DATE '2025-08-10',
  30000.00::decimal,
  CASE WHEN p.rn % 2 = 0 THEN 30000.00::decimal ELSE 19000.00::decimal END,
  CASE WHEN p.rn % 2 = 0 THEN 0.00::decimal ELSE 11000.00::decimal END,
  DATE '2025-08-30',
  CASE WHEN p.rn % 2 = 0 THEN 'Paid' ELSE 'Partial' END,
  CURRENT_TIMESTAMP
FROM picked p
JOIN first_inst fi ON fi.branch_id = p.branch_id;

INSERT INTO invoice_items (
  "Id", "InvoiceId", "FeeHeadId", "InstallmentId",
  "TotalAmount", "PaidAmount", "RemainingAmount", "CreatedAt"
)
SELECT
  uuid_generate_v5('6ba7b811-9dad-11d1-80b4-00c04fd430c8'::uuid, 'scenario-fee-line-t-' || inv."StudentId"::text),
  inv."Id",
  '30000001-0000-4000-8000-000000000001'::uuid,
  inv."InstallmentId",
  15000.00::decimal,
  CASE WHEN inv."PaidAmount" >= 30000.00 THEN 15000.00::decimal ELSE 9500.00::decimal END,
  CASE WHEN inv."PaidAmount" >= 30000.00 THEN 0.00::decimal ELSE 5500.00::decimal END,
  CURRENT_TIMESTAMP
FROM invoices inv
WHERE inv."InvoiceNo" LIKE 'SCENARIO-FEE-%';

INSERT INTO invoice_items (
  "Id", "InvoiceId", "FeeHeadId", "InstallmentId",
  "TotalAmount", "PaidAmount", "RemainingAmount", "CreatedAt"
)
SELECT
  uuid_generate_v5('6ba7b811-9dad-11d1-80b4-00c04fd430c8'::uuid, 'scenario-fee-line-tr-' || inv."StudentId"::text),
  inv."Id",
  '30000001-0000-4000-8000-000000000002'::uuid,
  inv."InstallmentId",
  9000.00::decimal,
  CASE WHEN inv."PaidAmount" >= 30000.00 THEN 9000.00::decimal ELSE 5700.00::decimal END,
  CASE WHEN inv."PaidAmount" >= 30000.00 THEN 0.00::decimal ELSE 3300.00::decimal END,
  CURRENT_TIMESTAMP
FROM invoices inv
WHERE inv."InvoiceNo" LIKE 'SCENARIO-FEE-%';

INSERT INTO invoice_items (
  "Id", "InvoiceId", "FeeHeadId", "InstallmentId",
  "TotalAmount", "PaidAmount", "RemainingAmount", "CreatedAt"
)
SELECT
  uuid_generate_v5('6ba7b811-9dad-11d1-80b4-00c04fd430c8'::uuid, 'scenario-fee-line-l-' || inv."StudentId"::text),
  inv."Id",
  '30000001-0000-4000-8000-000000000003'::uuid,
  inv."InstallmentId",
  6000.00::decimal,
  CASE WHEN inv."PaidAmount" >= 30000.00 THEN 6000.00::decimal ELSE 3800.00::decimal END,
  CASE WHEN inv."PaidAmount" >= 30000.00 THEN 0.00::decimal ELSE 2200.00::decimal END,
  CURRENT_TIMESTAMP
FROM invoices inv
WHERE inv."InvoiceNo" LIKE 'SCENARIO-FEE-%';

INSERT INTO payments (
  "Id", "InvoiceId", "StudentId", "BranchId", "DivisionId", "InstallmentId",
  "PaymentDate", "Amount", "PaymentMethod", "PaymentType", "TransactionNumber", "CreatedAt"
)
SELECT
  uuid_generate_v5('6ba7b811-9dad-11d1-80b4-00c04fd430c8'::uuid, 'scenario-fee-pay-' || inv."StudentId"::text),
  inv."Id",
  inv."StudentId",
  inv."BranchId",
  inv."DivisionId",
  inv."InstallmentId",
  DATE '2025-08-12',
  inv."PaidAmount",
  'Cash',
  'Fee',
  'SCENARIO-FEE-' || inv."StudentId"::text,
  CURRENT_TIMESTAMP
FROM invoices inv
WHERE inv."InvoiceNo" LIKE 'SCENARIO-FEE-%';

INSERT INTO payment_fee_lines ("Id", "PaymentId", "FeeHeadId", "Amount")
SELECT
  uuid_generate_v5('6ba7b811-9dad-11d1-80b4-00c04fd430c8'::uuid, 'scenario-pfl-a-' || pay."StudentId"::text),
  pay."Id",
  '30000001-0000-4000-8000-000000000001'::uuid,
  CASE WHEN inv."PaidAmount" >= 30000.00 THEN 15000.00::decimal ELSE 9500.00::decimal END
FROM payments pay
JOIN invoices inv ON pay."InvoiceId" = inv."Id"
WHERE pay."TransactionNumber" LIKE 'SCENARIO-FEE-%';

INSERT INTO payment_fee_lines ("Id", "PaymentId", "FeeHeadId", "Amount")
SELECT
  uuid_generate_v5('6ba7b811-9dad-11d1-80b4-00c04fd430c8'::uuid, 'scenario-pfl-b-' || pay."StudentId"::text),
  pay."Id",
  '30000001-0000-4000-8000-000000000002'::uuid,
  CASE WHEN inv."PaidAmount" >= 30000.00 THEN 9000.00::decimal ELSE 5700.00::decimal END
FROM payments pay
JOIN invoices inv ON pay."InvoiceId" = inv."Id"
WHERE pay."TransactionNumber" LIKE 'SCENARIO-FEE-%';

INSERT INTO payment_fee_lines ("Id", "PaymentId", "FeeHeadId", "Amount")
SELECT
  uuid_generate_v5('6ba7b811-9dad-11d1-80b4-00c04fd430c8'::uuid, 'scenario-pfl-c-' || pay."StudentId"::text),
  pay."Id",
  '30000001-0000-4000-8000-000000000003'::uuid,
  CASE WHEN inv."PaidAmount" >= 30000.00 THEN 6000.00::decimal ELSE 3800.00::decimal END
FROM payments pay
JOIN invoices inv ON pay."InvoiceId" = inv."Id"
WHERE pay."TransactionNumber" LIKE 'SCENARIO-FEE-%';

-- ---------------------------------------------------------------------------
-- Exams: types + subjects; one term + one unit exam on first / second campuses
-- ---------------------------------------------------------------------------
INSERT INTO exam_types ("Id", "Name", "Description")
VALUES
  ('30000050-0000-4000-8000-000000000001'::uuid, 'Scenario Term', 'Scenario seed'),
  ('30000050-0000-4000-8000-000000000002'::uuid, 'Scenario Unit Test', 'Scenario seed');

INSERT INTO subjects ("Id", "Name", "Code")
VALUES
  ('30000051-0000-4000-8000-000000000001'::uuid, 'Mathematics', 'SCENARIO-MATH'),
  ('30000051-0000-4000-8000-000000000002'::uuid, 'English', 'SCENARIO-ENG'),
  ('30000051-0000-4000-8000-000000000003'::uuid, 'Science', 'SCENARIO-SCI');

WITH grp AS (
  SELECT
    b."Id" AS bid,
    b."Name" AS bname,
    row_number() OVER (ORDER BY b."Name") AS brn
  FROM branches b
  JOIN school_groups sg ON b."GroupId" = sg."Id"
  WHERE (sg."Code" = 'SCENARIO-GROUP' OR sg."Name" = 'Scenario Apex School Group')
),
cls1 AS (
  SELECT
    c."Id" AS cid,
    c."BranchId",
    row_number() OVER (PARTITION BY c."BranchId" ORDER BY c."Name") AS cn
  FROM classes c
)
INSERT INTO exams (
  "Id", "Name", "ExamTypeId", "AcademicYearId", "BranchId", "ClassId", "DivisionId",
  "StartDate", "EndDate", "Status", "IsLocked", "CreatedAt"
)
SELECT
  uuid_generate_v5('6ba7b811-9dad-11d1-80b4-00c04fd430c8'::uuid, 'scenario-exam-term-' || g.bid::text),
  'Scenario Exam Term - ' || g.bname,
  '30000050-0000-4000-8000-000000000001'::uuid,
  '10000004-0000-4000-8000-000000000001'::uuid,
  g.bid,
  c.cid,
  NULL,
  DATE '2026-02-10',
  DATE '2026-02-12',
  'Published',
  FALSE,
  CURRENT_TIMESTAMP
FROM grp g
JOIN cls1 c ON c."BranchId" = g.bid AND c.cn = 1
WHERE g.brn = 1
  AND EXISTS (
    SELECT 1 FROM academic_years ay WHERE ay."Id" = '10000004-0000-4000-8000-000000000001'::uuid
  );

WITH grp AS (
  SELECT
    b."Id" AS bid,
    b."Name" AS bname,
    row_number() OVER (ORDER BY b."Name") AS brn
  FROM branches b
  JOIN school_groups sg ON b."GroupId" = sg."Id"
  WHERE (sg."Code" = 'SCENARIO-GROUP' OR sg."Name" = 'Scenario Apex School Group')
),
cls1 AS (
  SELECT
    c."Id" AS cid,
    c."BranchId",
    row_number() OVER (PARTITION BY c."BranchId" ORDER BY c."Name") AS cn
  FROM classes c
)
INSERT INTO exams (
  "Id", "Name", "ExamTypeId", "AcademicYearId", "BranchId", "ClassId", "DivisionId",
  "StartDate", "EndDate", "Status", "IsLocked", "CreatedAt"
)
SELECT
  uuid_generate_v5('6ba7b811-9dad-11d1-80b4-00c04fd430c8'::uuid, 'scenario-exam-unit-' || g.bid::text),
  'Scenario Exam Unit - ' || g.bname,
  '30000050-0000-4000-8000-000000000002'::uuid,
  '10000004-0000-4000-8000-000000000001'::uuid,
  g.bid,
  c.cid,
  NULL,
  DATE '2026-01-20',
  DATE '2026-01-20',
  'Published',
  FALSE,
  CURRENT_TIMESTAMP
FROM grp g
JOIN cls1 c ON c."BranchId" = g.bid AND c.cn = 1
WHERE g.brn = 2
  AND EXISTS (
    SELECT 1 FROM academic_years ay WHERE ay."Id" = '10000004-0000-4000-8000-000000000001'::uuid
  );

INSERT INTO exam_subjects (
  "Id", "ExamId", "SubjectId", "TotalMaxMarks", "TotalPassMarks", "ExamDate", "DurationMinutes", "CreatedAt"
)
SELECT
  uuid_generate_v5('6ba7b811-9dad-11d1-80b4-00c04fd430c8'::uuid, 'scenario-esubj-' || e."Id"::text || '-' || s.sid::text),
  e."Id",
  s.sid,
  s.mx,
  s.pm,
  s.dt,
  s.dur,
  CURRENT_TIMESTAMP
FROM exams e
CROSS JOIN (
  VALUES
    ('30000051-0000-4000-8000-000000000001'::uuid, 100.00::numeric, 40.00::numeric, DATE '2026-02-10', 180),
    ('30000051-0000-4000-8000-000000000002'::uuid, 100.00::numeric, 40.00::numeric, DATE '2026-02-11', 180),
    ('30000051-0000-4000-8000-000000000003'::uuid, 100.00::numeric, 40.00::numeric, DATE '2026-02-12', 120)
) AS s(sid, mx, pm, dt, dur)
WHERE e."Name" LIKE 'Scenario Exam Term %';

INSERT INTO exam_subjects (
  "Id", "ExamId", "SubjectId", "TotalMaxMarks", "TotalPassMarks", "ExamDate", "DurationMinutes", "CreatedAt"
)
SELECT
  uuid_generate_v5('6ba7b811-9dad-11d1-80b4-00c04fd430c8'::uuid, 'scenario-esubj-' || e."Id"::text || '-' || s.sid::text),
  e."Id",
  s.sid,
  s.mx,
  s.pm,
  s.dt,
  s.dur,
  CURRENT_TIMESTAMP
FROM exams e
CROSS JOIN (
  VALUES
    ('30000051-0000-4000-8000-000000000001'::uuid, 50.00::numeric, 20.00::numeric, DATE '2026-01-20', 60),
    ('30000051-0000-4000-8000-000000000002'::uuid, 50.00::numeric, 20.00::numeric, DATE '2026-01-20', 60)
) AS s(sid, mx, pm, dt, dur)
WHERE e."Name" LIKE 'Scenario Exam Unit %';

-- Mark entry chain: component types → one "Written" component per exam_subject → sample marks for 30 students
INSERT INTO exam_component_types ("Id", "Name", "Code", "CreatedAt")
VALUES
  ('30000053-0000-4000-8000-000000000001'::uuid, 'Scenario Written', 'SCN-WRT', CURRENT_TIMESTAMP),
  ('30000053-0000-4000-8000-000000000002'::uuid, 'Scenario Oral', 'SCN-ORL', CURRENT_TIMESTAMP);

INSERT INTO exam_subject_components (
  "Id", "ExamSubjectId", "ComponentTypeId", "MaxMarks", "PassMarks", "WeightagePercent", "CreatedAt"
)
SELECT
  uuid_generate_v5('6ba7b811-9dad-11d1-80b4-00c04fd430c8'::uuid, 'scenario-esc-' || es."Id"::text),
  es."Id",
  '30000053-0000-4000-8000-000000000001'::uuid,
  es."TotalMaxMarks",
  es."TotalPassMarks",
  100.00::numeric,
  CURRENT_TIMESTAMP
FROM exam_subjects es
JOIN exams ex ON es."ExamId" = ex."Id"
WHERE ex."Name" LIKE 'Scenario Exam %';

INSERT INTO marks (
  "Id",
  "ExamSubjectComponentId",
  "StudentId",
  "MarksObtained",
  "IsAbsent",
  "IsMedical",
  "IsExempt",
  "Remark",
  "EnteredAt"
)
SELECT
  uuid_generate_v5('6ba7b811-9dad-11d1-80b4-00c04fd430c8'::uuid, 'scenario-mark-' || esc."Id"::text || '-' || st."Id"::text),
  esc."Id",
  st."Id",
  CASE
    WHEN mod(abs(hashtext(st."Id"::text || esc."Id"::text)), 19) = 0 THEN NULL
    ELSE round(
      (esc."MaxMarks" * (0.50 + (mod(abs(hashtext(st."Id"::text || esc."Id"::text)), 40))::numeric / 100.0))::numeric,
      2
    )
  END,
  (mod(abs(hashtext(st."Id"::text || esc."Id"::text)), 19) = 0),
  FALSE,
  FALSE,
  'SCENARIO-MARK-SEED',
  CURRENT_TIMESTAMP
FROM exam_subject_components esc
JOIN exam_subjects es ON esc."ExamSubjectId" = es."Id"
JOIN exams ex ON es."ExamId" = ex."Id"
JOIN (
  SELECT
    s."Id",
    row_number() OVER (ORDER BY s."AdmissionNo") AS rn
  FROM students s
  JOIN branches b ON s."BranchId" = b."Id"
  JOIN school_groups sg ON b."GroupId" = sg."Id"
  WHERE (sg."Code" = 'SCENARIO-GROUP' OR sg."Name" = 'Scenario Apex School Group')
) st ON st.rn <= 30
WHERE ex."Name" LIKE 'Scenario Exam %'
  AND esc."ComponentTypeId" = '30000053-0000-4000-8000-000000000001'::uuid;

COMMIT;
