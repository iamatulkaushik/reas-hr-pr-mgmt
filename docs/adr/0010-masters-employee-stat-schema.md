# ADR 0010: Masters, employee and statutory schema

Status: accepted. Owner: Sigma. Written Oct 2026 after reviewing the older 19-script set (`docs/sql-intake/README.md`).

## Decisions
- **Lanes.** 0010-0013 shared (cfg, aud, Reveal action). 0100-0108 masters and employees (0102 and 0104 are unused numbers). 0203 employee key on attendance. 0400-0401 statutory rates.
  Sigma wrote 0100-0108 because Phase 3 was blocked on them. Brave extends the lane from 0109 on (education, experience, contract, confirmation, shift history, promotion, increment, transfer, training, medical, asset, rehire).
- **Cross-company safety in the database.** Every parent has `UNIQUE (CompanyID, <ID>)` and every child uses a composite foreign key `(CompanyID, <ID>)`. A row of one company cannot point at another company's row.
- **Hierarchy.** Factory, then department (belongs to a factory), then section. An employee's department must belong to the employee's factory (composite key). Factory has the state: it drives professional tax and labour limits.
- **Protected ids.** UAN, ESI IP, PAN, Aadhaar, bank account and document numbers are ciphertext made by the application. Columns: `<Id>Enc`, `<Id>Last4`, and for the four employee ids a keyed hash `<Id>Hash` with a filtered unique index per company (duplicate check without decrypting). `IdKeyVersion` is one value per row, so a rotation re-encrypts the whole row. The database holds no key.
- **Reveal.** New permission action 8 `Reveal` (Owner and Associate on Employees). `*_Get` procedures return last four only. `*_GetEncrypted` procedures return ciphertext. The service checks `Reveal` and writes `aud.RevealLog` through `aud.usp_Reveal_Insert`.
- **Employee code.** `PREFIX-00042`, prefix from `mst.CompanyProfile`, number from a locked per-company counter. A legacy code can be passed in for imports.
- **Name.** One `FullName` (100 characters), as the Employees module validates it.
- **Statutory rates.** Typed tables, effective-dated by `EffectiveFrom` (no end date, so no overlaps). TDS by financial year and regime. Every row has `Verified`, `VerifiedOn`, `SourceRef`. **No rates are loaded.** Rows come in later migrations after CA sign-off; `cfg.Setting RateVerificationRequired = 1` makes payroll refuse unverified rows.
- **Reads by company.** Masters and employee procedures filter on `SESSION_CONTEXT` company in the query as well as through the tenant policy.
- **Guard 6.** `check-guards.sh` fails a table with a `CompanyID` column that is not in a tenant security policy (exempt: `mst.Company`, `sec.[User]`).

## Not decided here (open)
- How the application encrypts and hashes (`IFieldCrypto`, algorithm mode, hash key storage). Needs a change request from Brave and a decision before Phase 3 screens save ids.
- Factory scope for Operators, year-end archive, Crystal data access: see the open list in `docs/sql-intake/README.md`.
