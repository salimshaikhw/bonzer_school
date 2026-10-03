-- ============================================================
-- BonzerSchool – Student table: Scholarship, RTE & welfare columns
-- Run once on your PostgreSQL database after students table exists
-- ============================================================

-- ------------------------------------------------------------
-- Scholarship
-- ------------------------------------------------------------
ALTER TABLE students ADD COLUMN IF NOT EXISTS has_scholarship BOOLEAN DEFAULT false;
COMMENT ON COLUMN students.has_scholarship IS 'Whether student has any scholarship';

ALTER TABLE students ADD COLUMN IF NOT EXISTS scholarship_type VARCHAR(100);
COMMENT ON COLUMN students.scholarship_type IS 'e.g. State Merit, Post-Matric, National Means-cum-Merit, Pre-Matric, Minority';

ALTER TABLE students ADD COLUMN IF NOT EXISTS scholarship_status VARCHAR(50);
COMMENT ON COLUMN students.scholarship_status IS 'e.g. Applied, Approved, Disbursed, Rejected';

ALTER TABLE students ADD COLUMN IF NOT EXISTS scholarship_amount DECIMAL(12, 2);
COMMENT ON COLUMN students.scholarship_amount IS 'Annual or per-year scholarship amount if applicable';

ALTER TABLE students ADD COLUMN IF NOT EXISTS scholarship_eligible BOOLEAN DEFAULT false;
COMMENT ON COLUMN students.scholarship_eligible IS 'Eligible for scholarship (by income/category/scheme)';

-- ------------------------------------------------------------
-- RTE (Right to Education)
-- ------------------------------------------------------------
ALTER TABLE students ADD COLUMN IF NOT EXISTS is_rte_seat BOOLEAN DEFAULT false;
COMMENT ON COLUMN students.is_rte_seat IS 'Admitted under RTE 25% EWS/DG quota';

ALTER TABLE students ADD COLUMN IF NOT EXISTS rte_eligible BOOLEAN DEFAULT false;
COMMENT ON COLUMN students.rte_eligible IS 'Eligible for RTE (income/category criteria)';

ALTER TABLE students ADD COLUMN IF NOT EXISTS rte_application_no VARCHAR(100);
COMMENT ON COLUMN students.rte_application_no IS 'RTE portal application/reference number';

ALTER TABLE students ADD COLUMN IF NOT EXISTS ews_dg_category VARCHAR(20);
COMMENT ON COLUMN students.ews_dg_category IS 'EWS = Economically Weaker Section, DG = Disadvantaged Group (SC/ST/OBC etc)';

-- ------------------------------------------------------------
-- Welfare & reporting (UDISE / minority / BPL)
-- ------------------------------------------------------------
ALTER TABLE students ADD COLUMN IF NOT EXISTS is_minority BOOLEAN DEFAULT false;
COMMENT ON COLUMN students.is_minority IS 'Belongs to minority community (religion-based)';

ALTER TABLE students ADD COLUMN IF NOT EXISTS is_bpl BOOLEAN DEFAULT false;
COMMENT ON COLUMN students.is_bpl IS 'Below Poverty Line (BPL) family';

ALTER TABLE students ADD COLUMN IF NOT EXISTS bpl_card_no VARCHAR(100);
COMMENT ON COLUMN students.bpl_card_no IS 'BPL card number if applicable';

ALTER TABLE students ADD COLUMN IF NOT EXISTS udise_code VARCHAR(50);
COMMENT ON COLUMN students.udise_code IS 'UDISE+ student code if assigned by school';

ALTER TABLE students ADD COLUMN IF NOT EXISTS single_girl_child BOOLEAN DEFAULT false;
COMMENT ON COLUMN students.single_girl_child IS 'Single girl child in family (for some scheme benefits)';

ALTER TABLE students ADD COLUMN IF NOT EXISTS first_graduate BOOLEAN DEFAULT false;
COMMENT ON COLUMN students.first_graduate IS 'First in family to pursue graduation (for schemes)';

-- ------------------------------------------------------------
-- Optional indexes for reporting
-- ------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_students_has_scholarship ON students(has_scholarship) WHERE has_scholarship = true;
CREATE INDEX IF NOT EXISTS idx_students_is_rte_seat ON students(is_rte_seat) WHERE is_rte_seat = true;
CREATE INDEX IF NOT EXISTS idx_students_scholarship_eligible ON students(scholarship_eligible) WHERE scholarship_eligible = true;
CREATE INDEX IF NOT EXISTS idx_students_rte_eligible ON students(rte_eligible) WHERE rte_eligible = true;
