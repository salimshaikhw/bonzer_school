-- Add soft delete columns to attendance table (run if not already applied)
ALTER TABLE attendance ADD COLUMN IF NOT EXISTS is_deleted boolean DEFAULT false;
ALTER TABLE attendance ADD COLUMN IF NOT EXISTS deleted_at timestamp without time zone NULL;
