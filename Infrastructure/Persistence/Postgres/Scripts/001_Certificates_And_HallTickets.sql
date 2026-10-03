-- ============================================================
-- BonzerSchool ERP: TC, Bonafide & Hall Ticket tables
-- Database-first: run this script on PostgreSQL then scaffold/use in EF
-- No foreign keys: ID columns only, no referential constraints
-- ============================================================
-- Requires: uuid-ossp extension (usually already enabled)
-- ============================================================

-- Ensure extension exists
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ------------------------------------------------------------
-- 1. TC (Transfer Certificate) Certificates
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tc_certificates (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    student_id          UUID NOT NULL,
    academic_year_id    UUID,
    class_id            UUID,
    division_id         UUID,
    branch_id           UUID,
    tc_number           VARCHAR(100) NOT NULL,
    sequence            INT NOT NULL DEFAULT 1,
    issue_date          DATE NOT NULL DEFAULT CURRENT_DATE,
    reason              VARCHAR(200),
    remarks             TEXT,
    created_at          TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_tc_certificates_tc_number UNIQUE (tc_number)
);

CREATE INDEX idx_tc_certificates_student ON tc_certificates(student_id);
CREATE INDEX idx_tc_certificates_academic_year_class ON tc_certificates(academic_year_id, class_id);
CREATE INDEX idx_tc_certificates_issue_date ON tc_certificates(issue_date);

COMMENT ON TABLE tc_certificates IS 'Transfer certificates; tc_number auto-generated from AcademicYear + Class + sequence';

-- ------------------------------------------------------------
-- 2. Bonafide Certificates
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
    created_at            TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_bonafide_certificates_bonafide_number UNIQUE (bonafide_number)
);

CREATE INDEX idx_bonafide_certificates_student ON bonafide_certificates(student_id);
CREATE INDEX idx_bonafide_certificates_academic_year_class ON bonafide_certificates(academic_year_id, class_id);
CREATE INDEX idx_bonafide_certificates_issue_date ON bonafide_certificates(issue_date);

COMMENT ON TABLE bonafide_certificates IS 'Bonafide certificates; bonafide_number auto-generated from AcademicYear + Class + sequence';

-- ------------------------------------------------------------
-- 3. Hall Tickets (exam admit / hall ticket)
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
    generated_at      TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_hall_tickets_student ON hall_tickets(student_id);
CREATE INDEX idx_hall_tickets_exam ON hall_tickets(exam_id);
CREATE UNIQUE INDEX idx_hall_tickets_student_exam ON hall_tickets(student_id, exam_id);

COMMENT ON TABLE hall_tickets IS 'Hall tickets for exams; ticket_number can be auto-generated per exam/class';
