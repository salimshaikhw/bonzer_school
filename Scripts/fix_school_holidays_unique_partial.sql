-- Run if adding a holiday fails with unique_violation even when the UI list is empty
-- (e.g. a soft-deleted row still blocked the old table-wide UNIQUE constraint).

ALTER TABLE school_holidays DROP CONSTRAINT IF EXISTS uq_school_holidays_branch_date;

DROP INDEX IF EXISTS uq_school_holidays_branch_date_active;

CREATE UNIQUE INDEX uq_school_holidays_branch_date_active
    ON school_holidays (branch_id, holiday_date)
    WHERE is_deleted IS NOT TRUE;
