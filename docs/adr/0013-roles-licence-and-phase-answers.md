# ADR 0013: Associate-centred roles, licence, and answers from Brave and the business

Status: accepted, items marked Open to confirm. Owner: Sigma. Date: 2026-10-14. Follows ADR 0012.

## Role model (changes ADR 0003 and the plan assumption "Owner = old Company role")
- The **Associate** is the working user and can work on **several companies**. An Associate can create a company and create the **Owner** login for it. The Owner is the company's own final approver.
- **Second approver is the Associate (HR).** Approve on Payroll is granted to Associate as well as Owner (migration 0014). The rule stays: the approver is never the person who submitted. A company needs two people who hold Approve (for example two Associates, or an Associate and the Owner) before dual approval can pass; if it does not have two, the screen says so.
- **Design for Sigma (Phase 2 extension, not built yet):**
  - a user-to-company link table `sec.UserCompany`, so one Associate belongs to many companies;
  - a company picker after login that sets the session company; row-level security stays "one company per session", unchanged;
  - provisioning procedures for an Associate to create a company and its Owner login (reuses 0007);
  - error 50002 ("No company in session") becomes the signal to show the picker, not a technical error. Brave maps it to the message "Select a company to continue."
- **Open, to confirm:** `PlanFeature.MultiCompany` (Platinum) no longer limits an Associate. The Developer's licence sets how many companies an Associate may hold (see Licence). The Platinum feature stays reserved for one Owner running a group of companies.

## Licence (replaces "offline key carrying the plan" for Phase 10)
- A separate licensing design, **before finalisation**, by module, database and user control. The **Developer issues the key under the Developer's own ID**, and it limits what the Associate can use: plans, modules, users, companies.
- A licence ADR is written before Phase 10. Until then no code checks a licence; `IEntitlementService` stays the only gate.
- **Crystal licence:** the business states the designer add-in and runtime are free for internal applications and the Viewer is free for end users. That is recorded as a **note, not a confirmation**: Revolution HRMS is delivered to client companies, which is not "internal". Open: someone must read SAP's current runtime redistribution terms, record the URL and date in ADR 0005, and confirm in writing before Phase 10. It does not block the technical spike.

## Rates and CR 0011
- CR 0011 accepted and delivered (migration 0402, `PtRule.IsDue`). See `docs/change-requests/0011-pt-due-months.md`.
- National rates loaded **unverified** (migration 0403, `docs/rates/national-rates-2026-10.md`). The EPF ceiling is now 25,000 from 17-09-2026, which splits September 2026. The CA's sign-off is recorded by a sign-off migration, not by an ad hoc update.
- The statutory-table batch CR (ADR 0012) also covers **leave encashment caps**. `stat.GratuityRule` already exists.

## Agent authentication (amends ADR 0012)
Answers: the agent runs on the company's server and **pulls** from devices; a company has 2 or 3 factory locations with more than one device each; the brand is not chosen.
- Devices do not authenticate. **The key belongs to the agent installation and is bound to one company.** An agent serving more than one company holds one key per company.
- Devices are registered rows under a company and a location (factory). Each punch carries its device and location.
- Associate maps one RFID card to one employee (Create and Edit on Biometric).
- Open: the device brand. Phase 8 cannot start without it.

## Brave's answers, recorded
- Phase 3 and 4 findings: 547 mapped; 50002 to be mapped as above; roster errors 50020 and 50021 mapped when the roster repository is written; no screens have been run, so the gates stay open.
- Migration script headers disagree with file names (0103, 0105, 0107, 0108). **Not renamed**: a merged script is never edited, and its checksum is stored. Harmless.
- Crystal spike not run (no Windows machine on Brave's side). The business's offline Windows environment runs it.
- Day-one salary components: basic, DA, HRA, conveyance, special allowance, overtime, plus company-defined components with the same flags. Owner confirms.
- Pay period is the calendar month. Open: the Owner's answer on a 26th-to-25th cycle. If yes, it needs its own change request.
- Payroll inputs: paid days, LOP days and OT hours from the month summary; a run needs a closed month.
- No professional tax states in Phase 5.
- Loans: fixed EMI on reducing balance first; the 75% EMI cap and the interest setting live in company settings, never as constants. Skip months and part-prepayment later.
- One regular run per company per month; off-cycle bonus and arrears runs in Phase 7.
- Declarations are entered by the Associate; employee self-service later.
- Bank file: the Owner chooses the bank first. ECR and ESIC formats: Brave gets the EPFO and ESIC help files before coding.
- Full and final first version: leave encashment (caps from rule rows), gratuity from `stat.GratuityRule`, notice pay recovered.

## Environment
- UI: custom-styled native controls, nothing bought (ADR 0006 closed).
- GitHub usernames in `CODEOWNERS` stay as the placeholders for now, so code-owner review cannot be enforced yet.
- The business tests in its offline environment first and pushes after. SQL Server Developer edition is there. The Linux CI database job in ADR 0012 is dropped for now.
