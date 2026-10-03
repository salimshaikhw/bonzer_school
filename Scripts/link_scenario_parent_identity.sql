-- Link scenario students' parent_users rows to the demo Identity parent login (parent@local).
-- Identity user must exist (start API once with appsettings.Development.json DemoUsers).
-- parent_users.Username is matched case-insensitively to the parent's email at sign-in.
--
-- Applies to first 8 SCENARIO-SEED students (adjust LIMIT as needed).

BEGIN;

UPDATE parent_users pu
SET "Username" = 'parent@local'
WHERE pu."StudentId" IN (
  SELECT s."Id"
  FROM students s
  JOIN branches b ON s."BranchId" = b."Id"
  WHERE b."Schooluid" LIKE 'SCENARIO-SEED%'
  ORDER BY s."AdmissionNo"
  LIMIT 8
);

COMMIT;
