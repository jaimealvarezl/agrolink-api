-- AgroLink Seed Data Script
-- Run this after the application has created the tables

-- Insert sample farm
INSERT INTO "Farms" ("Name", "Location", "CreatedAt") 
VALUES ('Finca San José', 'Boaco, Nicaragua', NOW());

-- Insert sample paddocks
INSERT INTO "Paddocks" ("Name", "FarmId", "CreatedAt") 
VALUES 
    ('Potrero Norte', 1, NOW()),
    ('Potrero Sur', 1, NOW()),
    ('Potrero Este', 1, NOW());

-- Insert sample lots
INSERT INTO "Lots" ("Name", "PaddockId", "Status", "CreatedAt") 
VALUES 
    ('Lote A', 1, 'ACTIVE', NOW()),
    ('Lote B', 1, 'ACTIVE', NOW()),
    ('Lote C', 2, 'ACTIVE', NOW()),
    ('Lote D', 2, 'ACTIVE', NOW()),
    ('Lote E', 3, 'MAINTENANCE', NOW());

-- Insert sample owners
INSERT INTO "Owners" ("Name", "Phone", "CreatedAt") 
VALUES 
    ('Juan Pérez', '+505 8888-1234', NOW()),
    ('María González', '+505 8888-5678', NOW()),
    ('Carlos Rodríguez', '+505 8888-9012', NOW());

-- Insert sample users
INSERT INTO "Users" ("Name", "Email", "PasswordHash", "Role", "IsActive", "CreatedAt") 
VALUES 
    ('Admin User', 'admin@agrolink.com', '$2a$11$example.hash.here', 'ADMIN', true, NOW()),
    ('Field Worker', 'worker@agrolink.com', '$2a$11$example.hash.here', 'WORKER', true, NOW());

-- Note: Password hashes above are examples - use actual BCrypt hashes in production
-- To generate: BCrypt.Net.BCrypt.HashPassword("your_password")