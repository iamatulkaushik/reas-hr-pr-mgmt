# Change request 0024: biometric users screen, punch attendance screen, and one write to mst.Employee

- Requested by: Brave, 2026-11-06. Owner decision (2026-11-06): devices change with the company and its needs, so people are managed as biometric users, not per device.
- Needed by module / phase: Biometric, Phase 8, steps 0503 and 0504.
- What is missing or wrong in the shared code:
  1. `Revolution.Modules.Biometric.UI` is new (in the solution) with two plain forms, `BiometricUsersForm` and `PunchAttendanceForm`, and `BiometricShellModule`. Nothing in the App uses them yet.
  2. `att.usp_BiometricUser_Assign` (migration 0504) writes `mst.Employee.BiometricID`, `UpdatedUtc` and `UpdatedByUserID`. The Employees module owns that table. The employee form already edits the same column; the new screen lets the Associate set it for many people, and link ids that devices reported to an employee, without opening each employee.
- Proposed signature or schema change:
  1. `Revolution.App`: project references to `Revolution.Modules.Biometric` and `Revolution.Modules.Biometric.UI`, and in `Program.vb` one line like the arrears one: `New BiometricShellModule(BiometricModule.Compose(services.Host))` in the shell module list. `BiometricModule.Compose(host)` needs nothing from the Employees export.
  2. `MenuLayoutModule`: two `AddScreen` lines: `biometric.users` ("Biometric users", Process, sort 560) and `biometric.punchattendance` ("Attendance from punches", Process, sort 565), both with `ModuleKey.Biometric` and `PlanFeature.Biometric`. `MenuIconMap`: two icon lines.
  3. Accept or change the write to `mst.Employee.BiometricID` in `att.usp_BiometricUser_Assign`. If Sigma prefers it, move the procedure into the `mst` lane with the same name and signature and Brave changes only the schema name in `SqlBiometricRepository`.
  4. Review of migrations 0503 (`att.usp_Punch_Times`) and 0504 (`usp_BiometricUser_List`, `_Unknown`, `_NextId`, `_Assign`), error number 53307.
  5. A device registration screen needs a location picker (the factory list). Brave will add it when Sigma says how a module reaches the factory list without referencing the Employees module.
- Workaround until done: none for the screens; the service and procedures are tested.
- Sigma's decision (accepted / changed / rejected) and date:
