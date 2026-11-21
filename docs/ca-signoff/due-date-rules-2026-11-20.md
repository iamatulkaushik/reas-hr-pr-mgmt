# Due date rules for the CA to verify (2026-11-20)

Loaded by migration 0417 into `stat.DueDateRule`, all with `IsVerified = 0`. Until a row is verified the Monthly Pack shows "Date not verified" and no date (ADR 0027).

| Item | Rule proposed | Source note |
|---|---|---|
| EPF contribution (and ECR) | by the 15th of the next month | to confirm on the EPFO site |
| ESIC contribution | by the 15th of the next month | to confirm on the ESIC portal |
| Tax deducted from salary (TDS) | by the 7th of the next month; 30 April for March | to confirm on the Income Tax site |

Not in this table: professional tax (due months in `stat.PtDueMonth`) and labour welfare (`stat.LabourWelfareRule`, Haryana due date still open in part G of the CA pack).

CA: please mark each row Verified, Not verified (give the correct rule) or Not applicable. Brave sets `IsVerified = 1` in a new migration once the CA signs.
