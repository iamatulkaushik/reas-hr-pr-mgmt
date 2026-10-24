# Change request 0013: Gratuity rule in Core

- Requested by: Brave
- Needed by module / phase: Phase 7 (full and final)
- What is missing or wrong in the shared code: `stat.GratuityRule` exists (migration 0400) but Core has no type for it and `IStatutoryRateService` has no member to read it.
- Proposed signature or schema change: Core `GratuityRule` with MinYearsRegular, MinYearsFixedTerm, DaysPerYear, MonthDivisor, RoundUpAfterMonths, CapAmount, Verified, VerifiedOn, SourceRef. `IStatutoryRateService.GetGratuityRule(asOf As Date) As GratuityRule`, the row in force on the date. Same rule as the other rates: an unverified row or a missing row is a failure, not a default. Read procedure `stat.usp_GratuityRule_Get @AsOf`.
- Workaround until done: `GratuityCalculator` takes a plain `GratuityTerms` object. When the Core type exists, I map it in one line and delete nothing else.
- Sigma's decision (accepted / changed / rejected) and date: **accepted as proposed, 2026-10-22.** Delivered: Core `GratuityRule` (all the fields you listed, `VerifiedOn` is `Date?`); `IStatutoryRateService.GetGratuityRule(asOf)`, which fails on a missing row and on an unverified row while verification is required, with a message the screen can show; `IStatutoryRateRepository.GetGratuityRule`; migration 0404 with `stat.usp_GratuityRule_Get @AsOf`; the SQL reader; four tests. Map it into `GratuityTerms` in one line as you planned. Nothing is seeded: the table stays empty until the CA supplies the row, so the first F&F run fails with the "no gratuity rule" message, as intended.
