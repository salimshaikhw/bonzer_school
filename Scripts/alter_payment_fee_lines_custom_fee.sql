-- Run against PostgreSQL before using ParentFees pay with custom fee lines (feeHeadId null + customFeeName).
-- Allows ad-hoc receipt lines and nullable fee_head_id on payment_fee_lines.

ALTER TABLE payment_fee_lines
    ALTER COLUMN fee_head_id DROP NOT NULL;

ALTER TABLE payment_fee_lines
    ADD COLUMN IF NOT EXISTS custom_fee_name character varying(200);
