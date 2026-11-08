# Change request 0025: the exact shape of the two agent wrapper procedures

- Requested by: Brave, 2026-11-07. Follows change request 0023, accepted by Sigma with the wrapper in the `agt` schema.
- Needed by module / phase: Biometric, Phase 8 step 0502 (`Revolution.Agent`).
- What is missing or wrong in the shared code: Sigma's reply fixes the procedure names but not the parameter types or the result columns. `Revolution.Agent` is written against the shape below, so Sigma's procedures must match it or this request must be changed.
- Proposed signature or schema change:
  1. `agt.usp_Agent_Devices(@AgentKey NVARCHAR(100))` returns one row per active device of the key's company with exactly these columns: `DeviceCode NVARCHAR(30)`, `Brand NVARCHAR(30)`, `Host NVARCHAR(100)`, `Port INT`, `LastPunchTime DATETIME2(0) NULL`.
  2. `agt.usp_Agent_Ingest(@AgentKey NVARCHAR(100), @DeviceCode NVARCHAR(30), @PunchesJson NVARCHAR(MAX))` returns one row with `Received INT`, `Inserted INT`, `Unmatched INT` (the three columns `att.usp_Punch_Ingest` already returns). The JSON is the array `att.usp_Punch_Ingest` reads.
  3. The agent key is sent as text: the base64 of the 32 random bytes. The wrapper hashes it and compares in constant time as Sigma described. If Sigma wants `VARBINARY(32)` instead, say so and Brave changes one line in `SqlAgentGateway`.
  4. Any failure (unknown key, retired key, unknown device code, a device code of another company) may raise one error with any number; the agent turns every `SqlException` into the same message and logs only that.
- Workaround until done: `RevolutionAgent once` fails with "The server did not accept the request." until the procedures exist.
- Sigma's decision (accepted / changed / rejected) and date:
