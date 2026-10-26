# Ownership

| Area | Owner |
|---|---|
| Revolution.Core, Revolution.Data, Revolution.Services, Revolution.UI.Common | **Sigma** |
| database/migrations, Directory.Build.props, Revolution.slnx, CI, docs/adr | **Sigma** |
| reports/ReportStudio (report engine, viewer, designer; vendored, see ADR 0005) | **Sigma**, changes go to its developer as a numbered request |
| Feature modules (employees, attendance, payroll, statutory, reports, biometric agent, assets) | **Brave** |
| Tests | whoever writes the code under test |

Brave needs a shared change: copy docs/change-requests/TEMPLATE.md, fill it in, ask Sigma. Do not edit the shared project.
Every pull request is reviewed by the other developer. main is protected.

Standing rules (ADR 0008):
- Brave may add `Revolution.Modules.*` project lines and their test projects to Revolution.slnx. No change request needed.
- Brave writes module repositories inside the module, using `IConnectionFactory` and `StoredProcedure`.
- Migration lanes: Sigma 0001-0099, masters and employees 0100-0199, attendance 0200-0299, payroll 0300-0399, statutory and tax 0400-0499, biometric 0500-0599, assets and expenses 0600-0699. Sigma reviews every migration.
