-- ============================================================
-- BonzerSchool ERP – TC, Bonafide, Hall Ticket, Student ID Card
-- With TC clearance workflow (no-due by department)
-- No foreign keys – ID columns only
-- ============================================================

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ============================================================
-- 1. DEPARTMENTS (NO-DUE CONFIGURATION)
-- ============================================================
-- Note: Your existing table is "department" (singular); this is "departments"
-- (plural) for branch-wise mandatory departments used in TC clearance.

CREATE TABLE IF NOT EXISTS departments (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    department_name VARCHAR(150) NOT NULL,
    branch_id       UUID NOT NULL,
    is_mandatory    BOOLEAN DEFAULT true,
    created_at      TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_departments_branch ON departments(branch_id);

-- ============================================================
-- 2. TRANSFER CERTIFICATES (TC)
-- ============================================================

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
    leaving_date                    DATE,
    last_class_attended             VARCHAR(100),
    conduct                         VARCHAR(100),

    last_exam_name_snapshot         VARCHAR(200),
    last_exam_grade_snapshot        VARCHAR(50),

    fees_cleared                    BOOLEAN DEFAULT false,

    status                          VARCHAR(40) NOT NULL DEFAULT 'DRAFT',
    -- DRAFT / PENDING_CLEARANCE / APPROVED / REJECTED / ISSUED

    is_duplicate                    BOOLEAN DEFAULT false,
    reason                          VARCHAR(250),
    remarks                         TEXT,

    issued_by_staff_id              UUID,
    approved_by_staff_id            UUID,
    approved_at                     TIMESTAMP,

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
-- 3. TC CLEARANCE WORKFLOW TABLE
-- ============================================================

CREATE TABLE IF NOT EXISTS tc_clearances (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

    tc_id               UUID NOT NULL,
    student_id          UUID NOT NULL,
    department_id       UUID NOT NULL,

    status              VARCHAR(30) NOT NULL DEFAULT 'PENDING',
    -- PENDING / CLEARED / REJECTED

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
-- 4. BONAFIDE CERTIFICATES
-- ============================================================

CREATE TABLE IF NOT EXISTS bonafide_certificates (
    id                      UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

    student_id              UUID NOT NULL,
    academic_year_id        UUID NOT NULL,
    class_id                UUID NOT NULL,
    division_id             UUID,
    branch_id               UUID NOT NULL,

    bonafide_number         VARCHAR(120) NOT NULL,
    sequence                INT NOT NULL,

    purpose                 VARCHAR(250),
    valid_from              DATE,
    valid_to                DATE,

    status                  VARCHAR(30) NOT NULL DEFAULT 'DRAFT',
    is_duplicate            BOOLEAN DEFAULT false,
    remarks                 TEXT,

    issued_by_staff_id      UUID,
    approved_by_staff_id    UUID,
    approved_at             TIMESTAMP,

    created_at              TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by              UUID,
    updated_at              TIMESTAMP,
    updated_by              UUID,
    deleted_at              TIMESTAMP,

    CONSTRAINT uq_bonafide_number UNIQUE (bonafide_number),
    CONSTRAINT uq_bonafide_sequence UNIQUE (academic_year_id, branch_id, sequence),
    CONSTRAINT chk_bonafide_dates CHECK (
        valid_to IS NULL OR valid_from IS NULL OR valid_from <= valid_to
    )
);

CREATE INDEX IF NOT EXISTS idx_bonafide_student ON bonafide_certificates(student_id);
CREATE INDEX IF NOT EXISTS idx_bonafide_branch_year ON bonafide_certificates(branch_id, academic_year_id);
CREATE INDEX IF NOT EXISTS idx_bonafide_status ON bonafide_certificates(status);

-- ============================================================
-- 5. HALL TICKETS
-- ============================================================

CREATE TABLE IF NOT EXISTS hall_tickets (
    id                          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

    student_id                  UUID NOT NULL,
    exam_id                     UUID NOT NULL,
    academic_year_id            UUID NOT NULL,
    class_id                    UUID NOT NULL,
    division_id                 UUID,
    branch_id                   UUID NOT NULL,

    ticket_number               VARCHAR(120) NOT NULL,
    sequence                    INT NOT NULL,
    seat_number                 VARCHAR(50),

    exam_name_snapshot          VARCHAR(200),
    exam_start_date_snapshot    DATE,
    exam_end_date_snapshot      DATE,
    exam_centre                 VARCHAR(250),

    status                      VARCHAR(30) NOT NULL DEFAULT 'GENERATED',
    printed_at                  TIMESTAMP,
    printed_by                  UUID,
    is_reprint                  BOOLEAN DEFAULT false,

    generated_at                TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    generated_by                UUID,
    updated_at                  TIMESTAMP,
    updated_by                  UUID,
    deleted_at                  TIMESTAMP,

    CONSTRAINT uq_hall_ticket_student_exam UNIQUE (student_id, exam_id),
    CONSTRAINT uq_hall_ticket_number UNIQUE (ticket_number),
    CONSTRAINT uq_hall_ticket_sequence UNIQUE (exam_id, branch_id, sequence)
);

CREATE INDEX IF NOT EXISTS idx_hall_tickets_student ON hall_tickets(student_id);
CREATE INDEX IF NOT EXISTS idx_hall_tickets_exam ON hall_tickets(exam_id);
CREATE INDEX IF NOT EXISTS idx_hall_tickets_status ON hall_tickets(status);

-- ============================================================
-- 6. STUDENT ID CARDS
-- ============================================================

CREATE TABLE IF NOT EXISTS student_id_cards (
    id                          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),

    student_id                  UUID NOT NULL,
    academic_year_id            UUID NOT NULL,
    class_id                    UUID NOT NULL,
    division_id                 UUID,
    branch_id                   UUID NOT NULL,

    card_number                 VARCHAR(120) NOT NULL,
    sequence                    INT NOT NULL,

    student_name_snapshot       VARCHAR(200),
    admission_no_snapshot       VARCHAR(100),
    roll_number_snapshot        VARCHAR(100),
    class_name_snapshot         VARCHAR(100),
    division_name_snapshot      VARCHAR(100),
    dob_snapshot                DATE,
    blood_group_snapshot        VARCHAR(10),
    parent_name_snapshot        VARCHAR(200),
    address_snapshot            TEXT,
    photo_url_snapshot          TEXT,

    valid_from                  DATE,
    valid_to                    DATE,

    status                      VARCHAR(30) NOT NULL DEFAULT 'GENERATED',
    printed_at                  TIMESTAMP,
    printed_by                  UUID,
    is_reprint                  BOOLEAN DEFAULT false,

    generated_at                TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    generated_by                UUID,
    updated_at                  TIMESTAMP,
    updated_by                  UUID,
    deleted_at                  TIMESTAMP,

    CONSTRAINT uq_id_card_number UNIQUE (card_number),
    CONSTRAINT uq_id_card_student_year UNIQUE (student_id, academic_year_id),
    CONSTRAINT uq_id_card_sequence UNIQUE (academic_year_id, branch_id, sequence)
);

CREATE INDEX IF NOT EXISTS idx_student_id_cards_student ON student_id_cards(student_id);
CREATE INDEX IF NOT EXISTS idx_student_id_cards_branch_year ON student_id_cards(branch_id, academic_year_id);

-- ============================================================
-- 7. FUNCTION: INITIATE TC CLEARANCE (idempotent)
-- ============================================================

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
    WHERE id = p_tc_id
      AND deleted_at IS NULL;

    IF v_student_id IS NULL THEN
        RAISE EXCEPTION 'TC not found or deleted: %', p_tc_id;
    END IF;

    INSERT INTO tc_clearances (tc_id, student_id, department_id)
    SELECT p_tc_id, v_student_id, d.id
    FROM departments d
    WHERE d.branch_id = v_branch_id
      AND d.is_mandatory = true
    ON CONFLICT (tc_id, department_id) DO NOTHING;

    UPDATE tc_certificates
    SET status = 'PENDING_CLEARANCE',
        updated_at = CURRENT_TIMESTAMP
    WHERE id = p_tc_id
      AND deleted_at IS NULL;
END;
$$;

-- ============================================================
-- 8. FUNCTION: CHECK CLEARANCE COMPLETE
-- ============================================================

CREATE OR REPLACE FUNCTION check_tc_clearance_complete(p_tc_id UUID)
RETURNS BOOLEAN
LANGUAGE plpgsql
AS $$
DECLARE
    v_pending_count INT;
BEGIN
    SELECT COUNT(*)
    INTO v_pending_count
    FROM tc_clearances
    WHERE tc_id = p_tc_id
      AND status <> 'CLEARED'
      AND deleted_at IS NULL;

    IF v_pending_count = 0 THEN
        UPDATE tc_certificates
        SET status = 'APPROVED',
            approved_at = CURRENT_TIMESTAMP,
            updated_at = CURRENT_TIMESTAMP
        WHERE id = p_tc_id
          AND deleted_at IS NULL;
        RETURN true;
    END IF;

    RETURN false;
END;
$$;
