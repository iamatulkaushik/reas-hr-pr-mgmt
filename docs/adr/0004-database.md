# ADR 0004: One database, one migrations folder

Status: accepted. Owner: Sigma.

- One database named HRMS with schemas cfg, sec, mst, att, pay, stat, aud.
- Tenant isolation by CompanyID with SESSION_CONTEXT and row-level security (Phase 2).
- All SQL lives in database/migrations as NNNN_name.sql. Four digits, lowercase, unique. Idempotent. Never edit a merged script.
- App login has EXECUTE only. sa disabled.
- Backups: SQL Agent jobs, nightly full plus 4-hourly differential.
- Sensitive IDs (Aadhaar, PAN, bank, UAN, ESI IP) are AES-256 encrypted in the application. TDE protects data files and backups.
- Statutory rates and slabs are effective-dated rows in stat.* tables, never constants in code.
