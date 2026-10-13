# Change request 0011: Professional tax due months

- Requested by: Brave
- Needed by module / phase: Phase 5 (payroll run, PT)
- What is missing or wrong in the shared code: `stat.PtRule` has `FrequencyID` (monthly, half-yearly, yearly) and one `SpecialMonth`, but no list of the months when tax is due. For a half-yearly or yearly state the payroll run cannot tell which month to deduct, or how much (the slab amount may be annual).
- Proposed signature or schema change: new table `stat.PtDueMonth (StateID, EffectiveFrom, MonthNo)` keyed to `stat.PtRule`, plus a read procedure, and `PtRule.DueMonths As IReadOnlyList(Of Integer)` in Core. Slab `TaxAmount` is documented as the amount per due month.
- Workaround until done: `PtCalculator` handles monthly rules. Other frequencies return a failure and the run stops with that message.
- Sigma's decision (accepted / changed / rejected) and date: pending
