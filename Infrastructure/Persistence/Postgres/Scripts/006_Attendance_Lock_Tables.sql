-- Run this script on your Postgres DB (database-first). Requires uuid-ossp.
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Attendance date locks: lock attendance by division/class for a specific date
CREATE TABLE IF NOT EXISTS attendance_date_locks (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    class_id uuid NULL,
    division_id uuid NULL,
    attendance_date date NULL,
    is_locked boolean NOT NULL DEFAULT false,
    locked_at timestamp without time zone NULL,
    unlocked_at timestamp without time zone NULL
);

CREATE INDEX IF NOT EXISTS idx_attendance_date_locks_division_date
    ON attendance_date_locks (division_id, attendance_date);

-- Attendance academic year locks: lock attendance for an entire academic year
CREATE TABLE IF NOT EXISTS attendance_academic_year_locks (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    academic_year_id uuid NULL,
    is_locked boolean NOT NULL DEFAULT false,
    locked_at timestamp without time zone NULL,
    unlocked_at timestamp without time zone NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_attendance_academic_year_locks_academic_year
    ON attendance_academic_year_locks (academic_year_id)
    WHERE academic_year_id IS NOT NULL;
