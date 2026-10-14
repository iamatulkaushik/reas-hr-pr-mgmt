# ADR 0014: Associates on several companies, and creating companies from the app

Status: accepted. Owner: Sigma. Date: 2026-10-14. Builds ADR 0013. Amends ADR 0004 (app login) and the Phase 2 role model.

## Decision
- **sec.UserCompany** links a user to the companies the user may open. Only Associates use it. An Associate may have no company yet, so `sec.[User].CompanyID` is no longer required for an Associate. Developer: none. Owner and Operator: one company, required.
- **Login.** One company: it opens at once. Several, or none: the session starts with company 0 and the plan Copper, and the app shows the company picker. `IAuthService.ListCompanies` and `SelectCompany` change the session company and plan. Row-level security is unchanged: one company per session. Error 50002 with an Associate means "no company chosen yet".
- **Creating a company and an Owner from the app.** The app login is still denied `mst.usp_Company_Create` and `sec.usp_User_Create`. It gets two narrow procedures instead:
  - `sec.usp_Associate_CreateCompany` creates a company on the **Copper** plan and links the caller to it.
  - `sec.usp_Associate_CreateOwner` creates the **Owner** login (role 2 only, password must be changed at first login) for a company the caller works on. **A company has one Owner** (migration 0017: unique index and error 50033). Owners are created only by an Associate, and no procedure changes a user's role, so an Operator is never turned into an Owner.
  - Both read `SESSION_CONTEXT UserID` and check **in the database** that this user is an active Associate (and, for the Owner, linked to the company). They cannot create an Associate, an Operator or a Developer. Errors 50030 to 50032.
- **Companies per Associate, by plan (migration 0018).** The Developer gives each Associate a plan with `set-associate-plan` in the setup tool. The plan caps the companies the Associate works on: **Copper 5, Silver 15, Gold 35, Platinum 90**. There is no unlimited plan, because unlimited use slows the system down: `cfg.PlanQuota.MaxCompanies` is required and capped at 500, so an unlimited value cannot be stored. An Associate without a plan counts as Copper. `sec.usp_Associate_CreateCompany` checks the count under a lock and stops with error 50034. Only the setup tool can change the plan (`sec.usp_Associate_SetPlan` is denied to the app). A company's own plan (what its screens may do) is separate and is still set by the Developer.
- **Service.** `IAssociateAdminService` needs Create on Security (granted to Associate in 0014), checks the input, hashes the temporary password and writes `CompanyCreated` and `OwnerCreated` to the audit trail.
- **Setup tool.** `sec.usp_User_Create` also links a new Associate to the company it is given.
- **Second approver.** Associate has Approve on Payroll (0014). Owner has Reopen on Payroll.

## Limits, stated plainly
- The app sets `UserID` itself, as it sets the company, so a modified app could claim another Associate's id. The database check stops anyone who is not an Associate and any Associate acting outside the companies they work on, but it cannot stop one Associate pretending to be another. This is the same trust level as tenancy (ADR 0007) and is why the app login stays EXECUTE-only.
- A new company is Copper. Raising it is the Developer's job. A Developer tool and the licence limit on how many companies an Associate may hold come with the licence design before Phase 10.
- There is no screen yet for an Owner to create the company's Operators, and no "switch company" menu after the first choice. Both belong to the shell work.

## What was built
Migrations 0014 (grants), 0015 (link table, procedures), 0016 (schema `rpt`, ADR 0012). `CompanySummary`, `IAuthService.ListCompanies/SelectCompany`, `IAssociateAdminService`, `IAssociateAdminRepository` in Core; `AssociateAdminService` and the `AuthService` change in Services; `SqlAssociateAdminRepository` and `SqlCompanyRepository.ListForUser` in Data; `CompanyPickerForm`, `NewCompanyForm`, `OwnerLoginForm` in UI.Common; wiring in `AppServices` and `Program`. Tests: Services (11 new), Database (4 new, need SQL Server).
