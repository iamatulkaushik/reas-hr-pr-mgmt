# Change request 0013: Gratuity rule in Core

- Requested by: Brave
- Needed by module / phase: Phase 7 (full and final)
- What is missing or wrong in the shared code: `stat.GratuityRule` exists (migration 0400) but Core has no type for it and `IStatutoryRateService` has no member to read it.
- Proposed signature or schema change: Core `GratuityRule` with MinYearsRegular, MinYearsFixedTerm, DaysPerYear, MonthDivisor, RoundUpAfterMonths, CapAmount, Verified, VerifiedOn, SourceRef. `IStatutoryRateService.GetGratuityRule(asOf As Date) As GratuityRule`, the row in force on the date. Same rule as the other rates: an unverified row or a missing row is a failure, not a default. Read procedure `stat.usp_GratuityRule_Get @AsOf`.
- Workaround until done: `GratuityCalculator` takes a plain `GratuityTerms` object. When the Core type exists, I map it in one line and delete nothing else.
- Sigma's decision (accepted / changed / rejected) and date: pending
