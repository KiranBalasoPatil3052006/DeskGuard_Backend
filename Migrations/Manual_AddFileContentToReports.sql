-- Migration: AddFileContentToReports
-- Date: 2026-07-21
-- Description: Adds file_content (bytea) and generator_name (varchar) columns 
-- to the reports table for storing actual PDF content and the user display name.

-- Add the file_content column (stores generated PDF bytes)
ALTER TABLE reports ADD COLUMN IF NOT EXISTS file_content bytea;

-- Add the generator_name column (caches the display name of the generating user)
ALTER TABLE reports ADD COLUMN IF NOT EXISTS generator_name varchar(255);
