-- Run against the local development database. All sample rows are rolled back.
BEGIN;
INSERT INTO organizations (id, name, slug, created_at) VALUES
('01900000-0000-7000-8000-000000000001', 'Schema test A', 'schema-smoke-a', now()),
('01900000-0000-7000-8000-000000000002', 'Schema test B', 'schema-smoke-b', now());
INSERT INTO users (id, email, password_hash, display_name, created_at) VALUES
('01900000-0000-7000-8000-000000000003', 'schema-smoke@example.invalid', 'test-only', 'Schema test', now());
INSERT INTO departments (id, organization_id, name) VALUES
('01900000-0000-7000-8000-000000000004', '01900000-0000-7000-8000-000000000002', 'Other organization');

DO $$
BEGIN
    BEGIN
        INSERT INTO users (id, email, password_hash, display_name, created_at) VALUES
        ('01900000-0000-7000-8000-000000000005', 'SCHEMA-SMOKE@example.invalid', 'test-only', 'Duplicate', now());
        RAISE EXCEPTION 'Case-insensitive email uniqueness failed';
    EXCEPTION WHEN unique_violation THEN
        RAISE NOTICE 'PASS: case-insensitive email uniqueness';
    END;

    BEGIN
        INSERT INTO organization_members
        (id, organization_id, user_id, role, status, job_title, department_id, joined_at) VALUES
        ('01900000-0000-7000-8000-000000000006', '01900000-0000-7000-8000-000000000001',
         '01900000-0000-7000-8000-000000000003', 'employee', 'active', 'Tester',
         '01900000-0000-7000-8000-000000000004', now());
        RAISE EXCEPTION 'Cross-organization foreign key protection failed';
    EXCEPTION WHEN foreign_key_violation THEN
        RAISE NOTICE 'PASS: cross-organization department assignment rejected';
    END;
END $$;
ROLLBACK;
