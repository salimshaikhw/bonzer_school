-- =============================================================================
-- BONZER SCHOOL - ATTENDANCE MODULE - FINAL DATABASE SCRIPT (PostgreSQL)
-- Run this script once on your target database (database-first).
-- Safe to re-run: uses IF NOT EXISTS / ADD COLUMN IF NOT EXISTS.
-- =============================================================================

-- Suppress "already exists, skipping" notices when re-running (optional)
SET client_min_messages = WARNING;

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- -----------------------------------------------------------------------------
-- 1. Attendance table: soft delete columns
-- -----------------------------------------------------------------------------
ALTER TABLE attendance ADD COLUMN IF NOT EXISTS is_deleted boolean DEFAULT false;
ALTER TABLE attendance ADD COLUMN IF NOT EXISTS deleted_at timestamp without time zone NULL;

-- -----------------------------------------------------------------------------
-- 2. Attendance date locks (lock by division/class for a date)
-- -----------------------------------------------------------------------------
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

-- -----------------------------------------------------------------------------
-- 3. Attendance academic year locks (freeze entire year)
-- -----------------------------------------------------------------------------
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

-- -----------------------------------------------------------------------------
-- 4. Leave requests (student leave applications)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS leave_requests (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    student_id uuid NOT NULL,
    from_date date NOT NULL,
    to_date date NOT NULL,
    leave_type varchar(100) NULL,
    reason text NULL,
    status varchar(20) NOT NULL DEFAULT 'Pending',
    requested_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    requested_by uuid NULL,
    approved_at timestamp without time zone NULL,
    approved_by uuid NULL,
    rejection_reason text NULL
);

CREATE INDEX IF NOT EXISTS idx_leave_requests_student ON leave_requests (student_id);
CREATE INDEX IF NOT EXISTS idx_leave_requests_status ON leave_requests (status);
CREATE INDEX IF NOT EXISTS idx_leave_requests_dates ON leave_requests (from_date, to_date);

-- -----------------------------------------------------------------------------
-- 5. Attendance correction requests
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS attendance_correction_requests (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    attendance_id uuid NOT NULL,
    reason text NOT NULL,
    status varchar(20) NOT NULL DEFAULT 'Pending',
    requested_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    requested_by uuid NULL,
    approved_at timestamp without time zone NULL,
    approved_by uuid NULL,
    rejection_reason text NULL
);

CREATE INDEX IF NOT EXISTS idx_attendance_correction_attendance ON attendance_correction_requests (attendance_id);
CREATE INDEX IF NOT EXISTS idx_attendance_correction_status ON attendance_correction_requests (status);

-- =============================================================================
-- END OF SCRIPT
-- =============================================================================
