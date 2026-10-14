# Change request 0011: Professional tax due months

- Requested by: Brave
- Needed by module / phase: Phase 5 (payroll run, PT)
- What is missing or wrong in the shared code: `stat.PtRule` has `FrequencyID` (monthly, half-yearly, yearly) and one `SpecialMonth`, but no list of the months when tax is due. For a half-yearly or yearly state the payroll run cannot tell which month to deduct, or how much (the slab amount may be annual).
- Proposed signature or schema change: new table `stat.PtDueMonth (StateID, EffectiveFrom, MonthNo)` keyed to `stat.PtRule`, plus a read procedure, and `PtRule.DueMonths As IReadOnlyList(Of Integer)` in Core. Slab `TaxAmount` is documented as the amount per due month.
- Workaround until done: `PtCalculator` handles monthly rules. Other frequencies return a failure and the run stops with that message.
- Sigma's decision (accepted / changed / rejected) and date: **accepted as proposed, 2026-10-14.** Delivered: migration 0402 (`stat.PtDueMonth`, `stat.usp_PtDueMonth_List`), `PtRule.DueMonths` and `PtRule.IsDue(month)` in Core, repository read, and a service check.
  - A monthly rule has no due-month rows; every month is due.
  - A half-yearly rule must list exactly 2 months and a yearly rule exactly 1. Otherwise `GetPtRule` fails with a message naming the state and date, so payroll stops instead of guessing.
  - Slab `TaxAmount` is the amount **per due month**. `SpecialMonthTax` still replaces it in the special month if that month is also due.
  - For Brave: `PtCalculator` should return 0 when `rule.IsDue(month)` is False, and the slab amount (or special-month amount) when it is True.
