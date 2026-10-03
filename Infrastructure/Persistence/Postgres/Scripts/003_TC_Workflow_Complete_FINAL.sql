-- ============================================================
-- BonzerSchool ERP – TC REQUEST & APPROVAL WORKFLOW
-- ============================================================
-- Flow: Student/Parent Request → Department Clearances (incl. Library HOD)
--       → Head Clerk → Class Teacher → Principal → Issued
-- No foreign keys – ID columns only
-- ============================================================

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ============================================================
-- 1. DEPARTMENTS (NO-DUE CONFIGURATION)
-- ============================================================
-- Used for TC clearance: Library, Fees, Lab, etc. (is_mandatory = true)

CREATE TABLE IF NOT EXISTS departments (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    department_name VARCHAR(150) NOT NULL,
    branch_id       UUID NOT NULL,
    is_mandatory    BOOLEAN DEFAULT true,
    created_at      TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_departments_branch ON departments(branch_id);

-- ============================================================
-- 2. TC CERTIFICATES (WITH FULL WORKFLOW COLUMNS)
-- ============================================================
-- Status flow: DRAFT → PENDING_DEPARTMENT_CLEARANCE → PENDING_HEAD_CLERK
--            → PENDING_CLASS_TEACHER → PENDING_PRINCIPAL → APPROVED → ISSUED
--            (or REJECTED at any stage)

CREATE TABLE IF NOT EXISTS tc_certificates (
    id                              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

    student_id                      UUID NOT NULL,
    academic_year_id                UUID NOT NULL,
    class_id                        UUID NOT NULL,
    division_id                     UUID,
    branch_id                       UUID NOT NULL,

    tc_number                       VARCHAR(120) NOT NULL,
    sequence                        INT NOT NULL,

    admission_date_snapshot         DATE,
    leaving_date                   DATE,
    last_class_attended             VARCHAR(100),
    conduct                         VARCHAR(100),
    last_exam_name_snapshot         VARCHAR(200),
    last_exam_grade_snapshot         VARCHAR(50),
    fees_cleared                    BOOLEAN DEFAULT false,

    -- Workflow status
    status                          VARCHAR(50) NOT NULL DEFAULT 'DRAFT',
    -- DRAFT | PENDING_DEPARTMENT_CLEARANCE | PENDING_HEAD_CLERK |
    -- PENDING_CLASS_TEACHER | PENDING_PRINCIPAL | APPROVED | REJECTED | ISSUED

    is_duplicate                    BOOLEAN DEFAULT false,
    reason                          VARCHAR(250),
    remarks                         TEXT,

    -- Class teacher (optional: pre-set who must approve)
    class_teacher_staff_id          UUID,

    -- Approval chain: Head Clerk → Class Teacher → Principal
    head_clerk_approved_by          UUID,
    head_clerk_approved_at          TIMESTAMP,

    class_teacher_approved_by       UUID,
    class_teacher_approved_at       TIMESTAMP,

    principal_approved_by           UUID,
    principal_approved_at           TIMESTAMP,

    issued_by_staff_id              UUID,
    issued_at                       TIMESTAMP,

    rejected_by                     UUID,
    rejected_at                     TIMESTAMP,
    rejection_reason                TEXT,

    created_at                      TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by                      UUID,
    updated_at                      TIMESTAMP,
    updated_by                      UUID,
    deleted_at                      TIMESTAMP,

    CONSTRAINT uq_tc_number UNIQUE (tc_number),
    CONSTRAINT uq_tc_sequence UNIQUE (academic_year_id, branch_id, sequence)
);

CREATE INDEX IF NOT EXISTS idx_tc_student ON tc_certificates(student_id);
CREATE INDEX IF NOT EXISTS idx_tc_branch_year ON tc_certificates(branch_id, academic_year_id);
CREATE INDEX IF NOT EXISTS idx_tc_status ON tc_certificates(status);

-- ============================================================
-- 3. TC CLEARANCES (DEPARTMENT / LIBRARY HOD NO-DUES)
-- ============================================================
-- One row per (tc_id, department_id). Each department clears or rejects.

CREATE TABLE IF NOT EXISTS tc_clearances (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

    tc_id               UUID NOT NULL,
    student_id          UUID NOT NULL,
    department_id       UUID NOT NULL,

    status              VARCHAR(30) NOT NULL DEFAULT 'PENDING',
    -- PENDING | CLEARED | REJECTED

    due_amount          NUMERIC(10,2) DEFAULT 0,
    remarks             TEXT,

    cleared_by          UUID,
    cleared_at          TIMESTAMP,

    created_at          TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at          TIMESTAMP,
    deleted_at          TIMESTAMP,

    CONSTRAINT uq_tc_department UNIQUE (tc_id, department_id)
);

CREATE INDEX IF NOT EXISTS idx_tc_clearance_tc ON tc_clearances(tc_id);
CREATE INDEX IF NOT EXISTS idx_tc_clearance_status ON tc_clearances(status);

-- ============================================================
-- 4. TC WORKFLOW LOG (AUDIT TRAIL + NOTIFICATION SOURCE)
-- ============================================================
-- Every status change and approval is logged. App can use this to
-- send notifications and show "pending for Head Clerk / Class Teacher / Principal".

CREATE TABLE IF NOT EXISTS tc_workflow_log (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

    tc_id               UUID NOT NULL,

    from_status         VARCHAR(50),
    to_status           VARCHAR(50) NOT NULL,

    actor_role          VARCHAR(40),
    -- CREATED | DEPARTMENT_CLEARANCE | HEAD_CLERK | CLASS_TEACHER | PRINCIPAL | REJECTED | ISSUED

    action_by_staff_id  UUID,
    action_at           TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    remarks             TEXT,

    department_id       UUID,
    -- Set when actor_role = DEPARTMENT_CLEARANCE (which dept cleared)
);

CREATE INDEX IF NOT EXISTS idx_tc_workflow_log_tc ON tc_workflow_log(tc_id);
CREATE INDEX IF NOT EXISTS idx_tc_workflow_log_action_at ON tc_workflow_log(action_at);

-- ============================================================
-- 5. FUNCTION: INITIATE TC REQUEST (Student/Parent requested TC)
-- ============================================================
-- Creates clearance rows for all mandatory departments (incl. Library).
-- Class teacher and departments get notification (app reads status + workflow log).

CREATE OR REPLACE FUNCTION initiate_tc_clearance(p_tc_id UUID)
RETURNS VOID
LANGUAGE plpgsql
AS $$
DECLARE
    v_student_id UUID;
    v_branch_id  UUID;
BEGIN
    SELECT student_id, branch_id
    INTO v_student_id, v_branch_id
    FROM tc_certificates
    WHERE id = p_tc_id AND deleted_at IS NULL;

    IF v_student_id IS NULL THEN
        RAISE EXCEPTION 'TC not found or deleted: %', p_tc_id;
    END IF;

    INSERT INTO tc_clearances (tc_id, student_id, department_id)
    SELECT p_tc_id, v_student_id, d.id
    FROM departments d
    WHERE d.branch_id = v_branch_id AND d.is_mandatory = true
    ON CONFLICT (tc_id, department_id) DO NOTHING;

    UPDATE tc_certificates
    SET status = 'PENDING_DEPARTMENT_CLEARANCE',
        updated_at = CURRENT_TIMESTAMP
    WHERE id = p_tc_id AND deleted_at IS NULL;

    INSERT INTO tc_workflow_log (tc_id, from_status, to_status, actor_role)
    VALUES (p_tc_id, 'DRAFT', 'PENDING_DEPARTMENT_CLEARANCE', 'CREATED');
END;
$$;

-- ============================================================
-- 6. FUNCTION: WHEN ALL DEPARTMENTS CLEARED → PENDING_HEAD_CLERK
-- ============================================================

CREATE OR REPLACE FUNCTION check_tc_clearance_complete(p_tc_id UUID)
RETURNS BOOLEAN
LANGUAGE plpgsql
AS $$
DECLARE
    v_pending_count INT;
    v_current_status VARCHAR(50);
BEGIN
    SELECT status INTO v_current_status
    FROM tc_certificates
    WHERE id = p_tc_id AND deleted_at IS NULL;

    IF v_current_status <> 'PENDING_DEPARTMENT_CLEARANCE' THEN
        RETURN false;
    END IF;

    SELECT COUNT(*)
    INTO v_pending_count
    FROM tc_clearances
    WHERE tc_id = p_tc_id
      AND status <> 'CLEARED'
      AND deleted_at IS NULL;

    IF v_pending_count = 0 THEN
        UPDATE tc_certificates
        SET status = 'PENDING_HEAD_CLERK',
            updated_at = CURRENT_TIMESTAMP
        WHERE id = p_tc_id AND deleted_at IS NULL;

        INSERT INTO tc_workflow_log (tc_id, from_status, to_status, actor_role)
        VALUES (p_tc_id, 'PENDING_DEPARTMENT_CLEARANCE', 'PENDING_HEAD_CLERK', 'DEPARTMENT_CLEARANCE');

        RETURN true;
    END IF;

    RETURN false;
END;
$$;

-- ============================================================
-- 7. FUNCTION: HEAD CLERK APPROVES → PENDING_CLASS_TEACHER
-- ============================================================

CREATE OR REPLACE FUNCTION tc_head_clerk_approve(p_tc_id UUID, p_staff_id UUID)
RETURNS VOID
LANGUAGE plpgsql
AS $$
DECLARE
    v_current VARCHAR(50);
BEGIN
    SELECT status INTO v_current
    FROM tc_certificates
    WHERE id = p_tc_id AND deleted_at IS NULL;

    IF v_current <> 'PENDING_HEAD_CLERK' THEN
        RAISE EXCEPTION 'TC is not pending Head Clerk approval. Current status: %', v_current;
    END IF;

    UPDATE tc_certificates
    SET status = 'PENDING_CLASS_TEACHER',
        head_clerk_approved_by = p_staff_id,
        head_clerk_approved_at = CURRENT_TIMESTAMP,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = p_tc_id AND deleted_at IS NULL;

    INSERT INTO tc_workflow_log (tc_id, from_status, to_status, actor_role, action_by_staff_id)
    VALUES (p_tc_id, 'PENDING_HEAD_CLERK', 'PENDING_CLASS_TEACHER', 'HEAD_CLERK', p_staff_id);
END;
$$;

-- ============================================================
-- 8. FUNCTION: CLASS TEACHER APPROVES → PENDING_PRINCIPAL
-- ============================================================

CREATE OR REPLACE FUNCTION tc_class_teacher_approve(p_tc_id UUID, p_staff_id UUID)
RETURNS VOID
LANGUAGE plpgsql
AS $$
DECLARE
    v_current VARCHAR(50);
BEGIN
    SELECT status INTO v_current
    FROM tc_certificates
    WHERE id = p_tc_id AND deleted_at IS NULL;

    IF v_current <> 'PENDING_CLASS_TEACHER' THEN
        RAISE EXCEPTION 'TC is not pending Class Teacher approval. Current status: %', v_current;
    END IF;

    UPDATE tc_certificates
    SET status = 'PENDING_PRINCIPAL',
        class_teacher_approved_by = p_staff_id,
        class_teacher_approved_at = CURRENT_TIMESTAMP,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = p_tc_id AND deleted_at IS NULL;

    INSERT INTO tc_workflow_log (tc_id, from_status, to_status, actor_role, action_by_staff_id)
    VALUES (p_tc_id, 'PENDING_CLASS_TEACHER', 'PENDING_PRINCIPAL', 'CLASS_TEACHER', p_staff_id);
END;
$$;

-- ============================================================
-- 9. FUNCTION: PRINCIPAL APPROVES → APPROVED (ready to issue)
-- ============================================================

CREATE OR REPLACE FUNCTION tc_principal_approve(p_tc_id UUID, p_staff_id UUID)
RETURNS VOID
LANGUAGE plpgsql
AS $$
DECLARE
    v_current VARCHAR(50);
BEGIN
    SELECT status INTO v_current
    FROM tc_certificates
    WHERE id = p_tc_id AND deleted_at IS NULL;

    IF v_current <> 'PENDING_PRINCIPAL' THEN
        RAISE EXCEPTION 'TC is not pending Principal approval. Current status: %', v_current;
    END IF;

    UPDATE tc_certificates
    SET status = 'APPROVED',
        principal_approved_by = p_staff_id,
        principal_approved_at = CURRENT_TIMESTAMP,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = p_tc_id AND deleted_at IS NULL;

    INSERT INTO tc_workflow_log (tc_id, from_status, to_status, actor_role, action_by_staff_id)
    VALUES (p_tc_id, 'PENDING_PRINCIPAL', 'APPROVED', 'PRINCIPAL', p_staff_id);
END;
$$;

-- ============================================================
-- 10. FUNCTION: PRINCIPAL ISSUES TC (final step)
-- ============================================================

CREATE OR REPLACE FUNCTION tc_principal_issue(p_tc_id UUID, p_staff_id UUID)
RETURNS VOID
LANGUAGE plpgsql
AS $$
DECLARE
    v_current VARCHAR(50);
BEGIN
    SELECT status INTO v_current
    FROM tc_certificates
    WHERE id = p_tc_id AND deleted_at IS NULL;

    IF v_current <> 'APPROVED' THEN
        RAISE EXCEPTION 'TC must be APPROVED before issue. Current status: %', v_current;
    END IF;

    UPDATE tc_certificates
    SET status = 'ISSUED',
        issued_by_staff_id = p_staff_id,
        issued_at = CURRENT_TIMESTAMP,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = p_tc_id AND deleted_at IS NULL;

    INSERT INTO tc_workflow_log (tc_id, from_status, to_status, actor_role, action_by_staff_id)
    VALUES (p_tc_id, 'APPROVED', 'ISSUED', 'PRINCIPAL', p_staff_id);
END;
$$;

-- ============================================================
-- 11. FUNCTION: REJECT TC (any stage)
-- ============================================================

CREATE OR REPLACE FUNCTION tc_reject(p_tc_id UUID, p_staff_id UUID, p_reason TEXT DEFAULT NULL)
RETURNS VOID
LANGUAGE plpgsql
AS $$
DECLARE
    v_current VARCHAR(50);
BEGIN
    SELECT status INTO v_current
    FROM tc_certificates
    WHERE id = p_tc_id AND deleted_at IS NULL;

    IF v_current IN ('REJECTED', 'ISSUED') THEN
        RAISE EXCEPTION 'TC cannot be rejected. Current status: %', v_current;
    END IF;

    UPDATE tc_certificates
    SET status = 'REJECTED',
        rejected_by = p_staff_id,
        rejected_at = CURRENT_TIMESTAMP,
        rejection_reason = p_reason,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = p_tc_id AND deleted_at IS NULL;

    INSERT INTO tc_workflow_log (tc_id, from_status, to_status, actor_role, action_by_staff_id, remarks)
    VALUES (p_tc_id, v_current, 'REJECTED', 'REJECTED', p_staff_id, p_reason);
END;
$$;

-- ============================================================
-- 12. FUNCTION: DEPARTMENT CLEARS OR REJECTS (for app to call after updating tc_clearances)
-- ============================================================
-- App updates tc_clearances: SET status='CLEARED' or 'REJECTED', cleared_by, cleared_at.
-- Then app calls check_tc_clearance_complete(tc_id).
-- Optional: helper to clear one department and auto-check if all done.

CREATE OR REPLACE FUNCTION tc_clearance_set(
    p_tc_clearance_id UUID,
    p_status VARCHAR(30),  -- 'CLEARED' or 'REJECTED'
    p_staff_id UUID,
    p_due_amount NUMERIC DEFAULT 0,
    p_remarks TEXT DEFAULT NULL
)
RETURNS BOOLEAN
LANGUAGE plpgsql
AS $$
DECLARE
    v_tc_id UUID;
    v_all_cleared BOOLEAN;
BEGIN
    SELECT tc_id INTO v_tc_id
    FROM tc_clearances
    WHERE id = p_tc_clearance_id AND deleted_at IS NULL;

    IF v_tc_id IS NULL THEN
        RAISE EXCEPTION 'Clearance record not found: %', p_tc_clearance_id;
    END IF;

    IF p_status NOT IN ('CLEARED', 'REJECTED') THEN
        RAISE EXCEPTION 'Status must be CLEARED or REJECTED';
    END IF;

    UPDATE tc_clearances
    SET status = p_status,
        due_amount = p_due_amount,
        remarks = p_remarks,
        cleared_by = p_staff_id,
        cleared_at = CURRENT_TIMESTAMP,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = p_tc_clearance_id AND deleted_at IS NULL;

    IF p_status = 'REJECTED' THEN
        UPDATE tc_certificates
        SET status = 'REJECTED',
            rejected_by = p_staff_id,
            rejected_at = CURRENT_TIMESTAMP,
            rejection_reason = COALESCE(p_remarks, 'Rejected by department'),
            updated_at = CURRENT_TIMESTAMP
        WHERE id = v_tc_id AND deleted_at IS NULL;
        RETURN false;
    END IF;

    SELECT check_tc_clearance_complete(v_tc_id) INTO v_all_cleared;
    RETURN v_all_cleared;
END;
$$;

-- ============================================================
-- WORKFLOW SUMMARY
-- ============================================================
-- 1. Student/Parent requests TC → Create tc_certificates (status DRAFT), get tc_number.
-- 2. Call initiate_tc_clearance(tc_id) → status = PENDING_DEPARTMENT_CLEARANCE.
--    Notify: Class Teacher + all mandatory departments (incl. Library HOD).
-- 3. Each department: tc_clearance_set(clearance_id, 'CLEARED', staff_id) or 'REJECTED'.
--    When all CLEARED, status becomes PENDING_HEAD_CLERK. Notify Head Clerk.
-- 4. Head Clerk: tc_head_clerk_approve(tc_id, staff_id) → PENDING_CLASS_TEACHER. Notify Class Teacher.
-- 5. Class Teacher: tc_class_teacher_approve(tc_id, staff_id) → PENDING_PRINCIPAL. Notify Principal.
-- 6. Principal: tc_principal_approve(tc_id, staff_id) → APPROVED.
-- 7. Principal: tc_principal_issue(tc_id, staff_id) → ISSUED.
-- Reject anytime: tc_reject(tc_id, staff_id, reason).
