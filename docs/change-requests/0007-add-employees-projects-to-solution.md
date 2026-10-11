# Change request 0007: Add Employees module and its tests to the solution

- Requested by: Brave
- Needed by module / phase: Phase 3 (masters and employees)
- What is missing or wrong in the shared code: Guard 4 fails any project not listed in Revolution.slnx. Sigma owns the file. Brave's two new projects cannot merge until it lists them.
- Proposed signature or schema change: add these lines to Revolution.slnx.

```xml
<Project Path="src/Revolution.Modules.Employees/Revolution.Modules.Employees.vbproj" />
<Project Path="tests/Revolution.Modules.Employees.Tests/Revolution.Modules.Employees.Tests.vbproj" />
```

  First line goes under `/src/`. Second goes under `/tests/`. Future modules will each need the same two-line change. If Sigma prefers, accept a standing rule: Brave may add `Revolution.Modules.*` project lines to the slnx without a request.
- Workaround until done: Brave's branch fails guard 4 only. Module builds clean alone (`dotnet build`, 0 warnings, 0 errors).
- Sigma's decision (accepted / changed / rejected) and date: **Accepted, 2026-10-11.** Projects are already listed in Revolution.slnx. Standing rule in docs/OWNERSHIP.md.
