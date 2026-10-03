-- Per-branch Saturday weekly-off pattern (calendar month ordinals).
-- 0 = all Saturdays working, 1 = every Saturday off, 2 = 2nd & 4th off, 3 = 1st, 3rd & 5th off.

ALTER TABLE branches
    ADD COLUMN IF NOT EXISTS attendance_saturday_pattern smallint NOT NULL DEFAULT 0;

UPDATE branches
SET attendance_saturday_pattern = 1
WHERE attendance_saturday_off IS TRUE
  AND attendance_saturday_pattern = 0;

COMMENT ON COLUMN branches.attendance_saturday_pattern IS
    'Saturday weekly-off: 0=none, 1=all Saturdays, 2=2nd and 4th of month, 3=1st 3rd 5th of month.';
