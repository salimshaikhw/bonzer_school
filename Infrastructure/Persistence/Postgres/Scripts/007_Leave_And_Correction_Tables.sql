-- Run on Postgres (database-first). Creates leave_requests and attendance_correction_requests.
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Leave requests (student leave applications)
CREATE TABLE IF NOT EXISTS leave_requests (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    student_id uuid NOT NULL,
    from_date date NOT NULL,
    to_date date NOT NULL,
    leave_type varchar(100) NULL,
    reason text NULL,
    status varchar(20) NOT NULL DEFAULT 'Pending', -- Pending, Approved, Rejected
    requested_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    requested_by uuid NULL,
    approved_at timestamp without time zone NULL,
    approved_by uuid NULL,
    rejection_reason text NULL
);

CREATE INDEX IF NOT EXISTS idx_leave_requests_student ON leave_requests (student_id);
CREATE INDEX IF NOT EXISTS idx_leave_requests_status ON leave_requests (status);
CREATE INDEX IF NOT EXISTS idx_leave_requests_dates ON leave_requests (from_date, to_date);

-- Attendance correction requests
CREATE TABLE IF NOT EXISTS attendance_correction_requests (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    attendance_id uuid NOT NULL,
    reason text NOT NULL,
    status varchar(20) NOT NULL DEFAULT 'Pending', -- Pending, Approved, Rejected
    requested_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    requested_by uuid NULL,
    approved_at timestamp without time zone NULL,
    approved_by uuid NULL,
    rejection_reason text NULL
);

CREATE INDEX IF NOT EXISTS idx_attendance_correction_attendance ON attendance_correction_requests (attendance_id);
CREATE INDEX IF NOT EXISTS idx_attendance_correction_status ON attendance_correction_requests (status);

-- Optional: attendance_audit_log for AuditAttendanceChange (or use existing audit_logs with module='Attendance')
-- Using existing audit_logs is sufficient; no new table required.
