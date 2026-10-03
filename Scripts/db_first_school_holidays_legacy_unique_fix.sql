-- =============================================================================
-- OPTIONAL — run only if saving a holiday fails with unique_violation while the
-- UI shows no active row (legacy table-wide UNIQUE on branch_id + holiday_date).
-- Do NOT run on a fresh DB that only ever used db_first_branch_weekly_off_and_school_holidays.sql
-- =============================================================================

SET client_min_messages TO WARNING;

ALTER TABLE school_holidays DROP CONSTRAINT IF EXISTS uq_school_holidays_branch_date;

DROP INDEX IF EXISTS uq_school_holidays_branch_date_active;

CREATE UNIQUE INDEX uq_school_holidays_branch_date_active
    ON school_holidays (branch_id, holiday_date)
    WHERE is_deleted IS NOT TRUE;

RESET client_min_messages;

SELECT 'db_first_school_holidays_legacy_unique_fix: completed successfully' AS migration_status;
