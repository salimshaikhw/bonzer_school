-- ============================================================
-- BonzerSchool ERP – TC, Bonafide & Hall Ticket (FINAL)
-- Enterprise-ready, no foreign keys (ID columns only)
-- Run once on your PostgreSQL database
-- ============================================================
-- Requires: uuid-ossp extension
-- ============================================================

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ------------------------------------------------------------
-- 1. TC (Transfer Certificate)
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tc_certificates (
    id                    UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    student_id            UUID NOT NULL,
    academic_year_id      UUID,
    class_id              UUID,
    division_id           UUID,
    branch_id             UUID,
    tc_number             VARCHAR(100) NOT NULL,
    sequence              INT NOT NULL DEFAULT 1,
    issue_date            DATE NOT NULL DEFAULT CURRENT_DATE,
    reason                VARCHAR(200),
    remarks               TEXT,
    is_duplicate          BOOLEAN NOT NULL DEFAULT false,
    issued_by_staff_id    UUID,
    conduct               VARCHAR(100),
    fees_cleared          BOOLEAN DEFAULT true,
    created_at            TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_tc_certificates_tc_number UNIQUE (tc_number)
);

CREATE INDEX IF NOT EXISTS idx_tc_certificates_student ON tc_certificates(student_id);
CREATE INDEX IF NOT EXISTS idx_tc_certificates_academic_year_class ON tc_certificates(academic_year_id, class_id);
CREATE INDEX IF NOT EXISTS idx_tc_certificates_issue_date ON tc_certificates(issue_date);
CREATE INDEX IF NOT EXISTS idx_tc_certificates_issued_by ON tc_certificates(issued_by_staff_id);

COMMENT ON TABLE tc_certificates IS 'Transfer certificates; tc_number auto-generated per academic year + class';

-- ------------------------------------------------------------
-- 2. Bonafide Certificate
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS bonafide_certificates (
    id                    UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    student_id            UUID NOT NULL,
    academic_year_id      UUID,
    class_id              UUID,
    division_id           UUID,
    branch_id             UUID,
    bonafide_number       VARCHAR(100) NOT NULL,
    sequence              INT NOT NULL DEFAULT 1,
    issue_date            DATE NOT NULL DEFAULT CURRENT_DATE,
    purpose               VARCHAR(200),
    remarks               TEXT,
    valid_from            DATE,
    valid_to              DATE,
    is_duplicate          BOOLEAN NOT NULL DEFAULT false,
    created_at            TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_bonafide_certificates_bonafide_number UNIQUE (bonafide_number)
);

CREATE INDEX IF NOT EXISTS idx_bonafide_certificates_student ON bonafide_certificates(student_id);
CREATE INDEX IF NOT EXISTS idx_bonafide_certificates_academic_year_class ON bonafide_certificates(academic_year_id, class_id);
CREATE INDEX IF NOT EXISTS idx_bonafide_certificates_issue_date ON bonafide_certificates(issue_date);
CREATE INDEX IF NOT EXISTS idx_bonafide_certificates_validity ON bonafide_certificates(valid_from, valid_to);

COMMENT ON TABLE bonafide_certificates IS 'Bonafide certificates; bonafide_number auto-generated per academic year + class';

-- ------------------------------------------------------------
-- 3. Hall Tickets
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS hall_tickets (
    id                UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    student_id        UUID NOT NULL,
    exam_id           UUID NOT NULL,
    academic_year_id  UUID,
    class_id          UUID,
    division_id       UUID,
    branch_id         UUID,
    ticket_number     VARCHAR(100),
    seat_number       VARCHAR(50),
    exam_centre       VARCHAR(200),
    generated_at      TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_hall_tickets_student_exam UNIQUE (student_id, exam_id)
);

CREATE INDEX IF NOT EXISTS idx_hall_tickets_student ON hall_tickets(student_id);
CREATE INDEX IF NOT EXISTS idx_hall_tickets_exam ON hall_tickets(exam_id);

COMMENT ON TABLE hall_tickets IS 'Hall tickets for exams; ticket_number and exam dates from exam/subjects in app';

-- ------------------------------------------------------------
-- UPGRADE: Add enterprise columns if tables already exist
-- (Safe to run; adds columns only when missing)
-- ------------------------------------------------------------
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'tc_certificates') THEN
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'tc_certificates' AND column_name = 'is_duplicate') THEN
            ALTER TABLE tc_certificates ADD COLUMN is_duplicate BOOLEAN NOT NULL DEFAULT false;
        END IF;
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'tc_certificates' AND column_name = 'issued_by_staff_id') THEN
            ALTER TABLE tc_certificates ADD COLUMN issued_by_staff_id UUID;
        END IF;
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'tc_certificates' AND column_name = 'conduct') THEN
            ALTER TABLE tc_certificates ADD COLUMN conduct VARCHAR(100);
        END IF;
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'tc_certificates' AND column_name = 'fees_cleared') THEN
            ALTER TABLE tc_certificates ADD COLUMN fees_cleared BOOLEAN DEFAULT true;
        END IF;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'bonafide_certificates') THEN
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'bonafide_certificates' AND column_name = 'valid_from') THEN
            ALTER TABLE bonafide_certificates ADD COLUMN valid_from DATE;
        END IF;
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'bonafide_certificates' AND column_name = 'valid_to') THEN
            ALTER TABLE bonafide_certificates ADD COLUMN valid_to DATE;
        END IF;
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'bonafide_certificates' AND column_name = 'is_duplicate') THEN
            ALTER TABLE bonafide_certificates ADD COLUMN is_duplicate BOOLEAN NOT NULL DEFAULT false;
        END IF;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'hall_tickets') THEN
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'hall_tickets' AND column_name = 'exam_centre') THEN
            ALTER TABLE hall_tickets ADD COLUMN exam_centre VARCHAR(200);
        END IF;
    END IF;
END $$;
