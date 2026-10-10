# ADR 0002: Single owner for shared code

Status: accepted. Owner: Sigma.

**Context.** The three earlier attempts drifted: two IAuthService interfaces, four 0001_init.sql scripts, test projects missing from the solution.

**Decision.**
- Sigma is the only person who edits: Revolution.Core (models, interfaces, enums), Revolution.Data, Revolution.Services (AppService and friends), Revolution.UI.Common (BaseForm, theme, controls) and database/.
- Brave owns every feature module and its forms.
- Brave asks for shared changes through a change request (docs/change-requests/NNNN-title.md). Sigma replies within one working day.
- CODEOWNERS plus branch protection enforce this. scripts/check-guards.sh catches duplicate interfaces, SQL outside database/migrations and projects missing from the solution.

**Consequence.** Sigma is the bottleneck for shared code, so Phase 1 publishes the v0.1 contract freeze early to unblock Brave.
