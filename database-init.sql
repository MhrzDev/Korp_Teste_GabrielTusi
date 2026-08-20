-- The default database is created by the PostgreSQL image.
-- Each microservice receives an isolated database, preserving service ownership.
SELECT 'CREATE DATABASE korp_inventory'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'korp_inventory')\gexec

SELECT 'CREATE DATABASE korp_billing'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'korp_billing')\gexec

