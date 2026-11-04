# Change request 0023: agent key and the wrapper that stores punches

- Requested by: Brave, 2026-11-04. Phase 8 step 0501; Owner decision 2026-11-04: eSSL first.
- Needed by module / phase: Biometric, Phase 8.
- What is missing or wrong in the shared code: ADR 0012 and 0013 give the agent its own login and a key bound to one installation and one company, with Sigma's tables and procedures; they do not exist yet. Brave built the punch tables, the storing procedure `att.usp_Punch_Ingest` and the module (migrations 0500 to 0502) and needs the part in front of it.
- Proposed signature or schema change:
  1. The agent key and registration tables (Sigma's lane) and a wrapper procedure, for example `sec.usp_Agent_Ingest(@AgentKey, @DeviceCode, @PunchesJson)`, that finds the key, checks it is active, takes the company from the registration, sets the company in the session and calls `att.usp_Punch_Ingest(@CompanyID, @DeviceID, @PunchesJson)`. The device is looked up by company and `DeviceCode`, so the agent never sees a numeric id from another company.
  2. `att.usp_Punch_Ingest` is not granted to the app login or to the agent login. Only the wrapper may call it. It takes the company as a parameter on purpose, and it expects the wrapper to have set `SESSION_CONTEXT` first, because the tenant filter policy (0501) hides rows otherwise.
  3. A way for the agent to read its own device list (code, brand, host, port, newest stored punch time) through a second wrapper, such as `sec.usp_Agent_Devices(@AgentKey)`, returning only that company's active devices. Brave's `PunchPullCoordinator` needs exactly those columns.
  4. Review of migrations 0500 (tables), 0501 (tenant policy) and 0502 (procedures), error numbers 53300 to 53306.
- Workaround until done: punches cannot be stored from outside a test. Devices, cards and the punch list work for a signed-in user.
- Sigma's decision (accepted / changed / rejected) and date:
