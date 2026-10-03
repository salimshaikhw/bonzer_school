-- Run against BonzerSchool Postgres DB. Requires uuid-ossp (already used by app).

ALTER TABLE branches
    ADD COLUMN IF NOT EXISTS attendance_saturday_off boolean NULL DEFAULT false;

ALTER TABLE branches
    ADD COLUMN IF NOT EXISTS attendance_sunday_off boolean NULL DEFAULT true;

COMMENT ON COLUMN branches.attendance_saturday_off IS 'When true, Saturday is weekly off for attendance.';
COMMENT ON COLUMN branches.attendance_sunday_off IS 'When true (default), Sunday is weekly off; false means Sunday works.';

CREATE TABLE IF NOT EXISTS school_holidays (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    branch_id uuid NOT NULL REFERENCES branches (id) ON DELETE CASCADE,
    holiday_date date NOT NULL,
    name varchar(200) NULL,
    created_at timestamp without time zone NULL DEFAULT CURRENT_TIMESTAMP,
    is_deleted boolean NULL DEFAULT false
);

-- One active holiday per branch per date (soft-deleted rows do not block re-add)
CREATE UNIQUE INDEX IF NOT EXISTS uq_school_holidays_branch_date_active
    ON school_holidays (branch_id, holiday_date)
    WHERE is_deleted IS NOT TRUE;

CREATE INDEX IF NOT EXISTS ix_school_holidays_branch_date
    ON school_holidays (branch_id, holiday_date)
    WHERE is_deleted IS DISTINCT FROM true;

-- Saturday pattern: 0=none, 1=all, 2=2nd&4th, 3=1st&3rd&5th (see Scripts/add_attendance_saturday_pattern.sql)
ALTER TABLE branches
    ADD COLUMN IF NOT EXISTS attendance_saturday_pattern smallint NOT NULL DEFAULT 0;

UPDATE branches
SET attendance_saturday_pattern = 1
WHERE attendance_saturday_off IS TRUE
  AND attendance_saturday_pattern = 0;
