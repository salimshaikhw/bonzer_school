-- =============================================================================
-- Database-first: branch weekly-off + Saturday pattern + school_holidays
-- =============================================================================
-- Safe to re-run. NOTICE lines like "already exists, skipping" are normal;
-- this script hides them so the output is quiet.
--
-- attendance_saturday_pattern: 0=None, 1=all Sat off, 2=2nd&4th, 3=1st&3rd&5th
-- =============================================================================

SET client_min_messages TO WARNING;

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

ALTER TABLE branches
    ADD COLUMN IF NOT EXISTS attendance_saturday_off boolean NULL DEFAULT false;

ALTER TABLE branches
    ADD COLUMN IF NOT EXISTS attendance_sunday_off boolean NULL DEFAULT true;

ALTER TABLE branches
    ADD COLUMN IF NOT EXISTS attendance_saturday_pattern smallint NOT NULL DEFAULT 0;

-- If an older script used integer, optionally normalize:
-- ALTER TABLE branches ALTER COLUMN attendance_saturday_pattern TYPE smallint;

UPDATE branches
SET attendance_saturday_pattern = 1
WHERE attendance_saturday_off IS TRUE
  AND attendance_saturday_pattern = 0;

COMMENT ON COLUMN branches.attendance_saturday_off IS
    'When true, Saturday is weekly off for attendance (legacy; pattern column preferred).';
COMMENT ON COLUMN branches.attendance_sunday_off IS
    'When true (default), Sunday is weekly off; false means Sunday is a working day.';
COMMENT ON COLUMN branches.attendance_saturday_pattern IS
    'Saturday weekly-off: 0=none, 1=all Saturdays, 2=2nd and 4th of month, 3=1st 3rd 5th of month.';

CREATE TABLE IF NOT EXISTS school_holidays (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    branch_id uuid NOT NULL REFERENCES branches (id) ON DELETE CASCADE,
    holiday_date date NOT NULL,
    name varchar(200) NULL,
    created_at timestamp without time zone NULL DEFAULT CURRENT_TIMESTAMP,
    is_deleted boolean NULL DEFAULT false
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_school_holidays_branch_date_active
    ON school_holidays (branch_id, holiday_date)
    WHERE is_deleted IS NOT TRUE;

CREATE INDEX IF NOT EXISTS ix_school_holidays_branch_date
    ON school_holidays (branch_id, holiday_date)
    WHERE is_deleted IS DISTINCT FROM true;

RESET client_min_messages;

-- Single visible row = success (no error). Re-runs also succeed.
SELECT 'db_first_branch_weekly_off_and_school_holidays: completed successfully' AS migration_status;
