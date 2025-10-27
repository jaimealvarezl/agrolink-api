-- AgroLink Database Creation Script
-- Run this script to create the database and initial user

-- Create database
CREATE DATABASE agrolink;

-- Create user (optional - adjust as needed)
-- CREATE USER agrolink_user WITH PASSWORD 'your_secure_password';
-- GRANT ALL PRIVILEGES ON DATABASE agrolink TO agrolink_user;

-- Connect to the database
\c agrolink;

-- Create extensions if needed
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Note: The actual tables will be created by Entity Framework
-- This script just sets up the database infrastructure

-- Optional: Create a read-only user for reporting
-- CREATE USER agrolink_readonly WITH PASSWORD 'readonly_password';
-- GRANT CONNECT ON DATABASE agrolink TO agrolink_readonly;
-- GRANT USAGE ON SCHEMA public TO agrolink_readonly;
-- GRANT SELECT ON ALL TABLES IN SCHEMA public TO agrolink_readonly;
-- ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT ON TABLES TO agrolink_readonly;