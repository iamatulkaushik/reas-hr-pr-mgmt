# Change request 0008: Add Attendance module and its tests to the solution

- Requested by: Brave
- Needed by module / phase: Phase 4 (attendance)
- What is missing or wrong in the shared code: Guard 4 fails any project not listed in Revolution.slnx. Sigma owns the file.
- Proposed signature or schema change: add these lines to Revolution.slnx.

```xml
<Project Path="src/Revolution.Modules.Attendance/Revolution.Modules.Attendance.vbproj" />
<Project Path="tests/Revolution.Modules.Attendance.Tests/Revolution.Modules.Attendance.Tests.vbproj" />
```

  First line goes under `/src/`. Second goes under `/tests/`. Asked again for a standing rule: Brave adds `Revolution.Modules.*` lines to the slnx without a request.
- Workaround until done: Brave's branch fails guard 4 only. Module builds clean alone.
- Sigma's decision (accepted / changed / rejected) and date:
