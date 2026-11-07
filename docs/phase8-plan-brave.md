# Phase 8 plan (Brave): biometric and RFID punches

Date: 2026-11-04. Based on origin/main `251a859` plus the Phase 7 step 7e-2b zip. Owner decision (2026-11-04): the first device brand is eSSL. The model and its protocol are not known yet.

## What the earlier decisions fix (ADR 0012, ADR 0013)
The agent is a Windows Service on the factory LAN and it pulls punches from the devices; devices do not log in and nothing reaches into the server. The agent key belongs to one installation and one company. A company has two or three locations with more than one device each, so every punch carries its device and its location. The Associate maps one RFID card to one employee (Create and Edit on Biometric, already seeded in 0014). The employee's `BiometricID` (0105) is the user id on the device.

## Steps
| Step | What | Status |
|---|---|---|
| 0500 | Tables `att.Device`, `att.RfidCard`, `att.Punch` (0500), tenant policy (0501), procedures (0502): save and list devices, map, unmap and list cards, ingest, list and resolve punches. No new schema: they sit in `att`. | **done 2026-11-04** |
| 0500 | `Revolution.Modules.Biometric`: `IPunchSource` (one reader per brand), `IPunchSink`, `PunchBatchBuilder` (checks and de-duplicates a pull), `PunchPullCoordinator` (one round over all active devices, a dead device does not stop the others), `BiometricService` (devices, cards, punches, resolve), 27 tests | **done 2026-11-04** |
| 0501 | Agent wrapper and key, in Sigma's lane: change request 0023 | waiting for Sigma |
| 0502 | `Revolution.Agent` (Windows Service) and `Revolution.Agent.Essl` (the eSSL reader behind `IPunchSource`). Models: eSSL K30 Pro and K90 Pro. | model known; waiting for the reader decision below and for Sigma's wrapper (CR 0023) |
| 0503 | Attendance from punches (Owner, 2026-11-04: **exit is marked automatically at the end of the assigned shift** when only one punch is found). `PunchDayBuilder` (first punch in, last punch out, middle punches ignored, double punches within 30 minutes count once, a night shift runs to the next day, window 3 hours before to 6 hours after the shift), `PunchAttendanceService` (Preview and Apply for at most nine days; marks only days with no mark; weekly off, holiday, closed month, no shift, a lone punch after the shift end and a shift that has not ended are listed and left alone; Apply goes through `AttendanceService.MarkDay`), migration 0503 (`att.usp_Punch_Times`), 35 tests | **done 2026-11-04**; the Owner has not said which shifts cross midnight, and the code does not need to know: it follows the shift's own start and end |
| 0504 | Biometric users, apart from devices (Owner, 2026-11-06): each employee has one biometric id, the same on every device, and one card; any device of any brand that reports them is matched. Migration 0504 (`usp_BiometricUser_List`, `_Unknown`, `_NextId`, `_Assign`), `BiometricService` methods (list users, list unknown ids and cards, next free id, assign and clear an id, link an unknown id or card to an employee and match the waiting punches), `Revolution.Modules.Biometric.UI` with `BiometricUsersForm` and `PunchAttendanceForm`, 9 tests, change request 0024 | **done 2026-11-06**; the device registration screen and the punch list screen are not built (a device screen needs a location picker, see change request 0024) |

## Rules built into step 0500
A punch is stored once: the same device, time, device user and card is ignored, so the agent may pull again from a few minutes before the newest stored punch (five minutes) and nothing doubles. A punch is matched to an employee when it arrives, by the active card first and then by `BiometricID`; it stays unmatched until `usp_Punch_Resolve` is run after a card is mapped or an id is set. A card on another active employee is refused; a new card for an employee retires the old one. A punch time more than a day ahead or more than three years back is thrown away and counted (a device that lost its clock reports 2000-01-01). Times are the device's own clock, the factory's local time.

## Questions for the Owner
1. Which eSSL model, and how does the company use it today: the eSSL software with its own database, or the devices alone on the LAN? The reader reads over the network (the ZK protocol on port 4370 is what most eSSL devices speak); if the devices only push to eSSL's own software, the reader reads that software's log table instead.
2. Do the devices have a comm key (password)? It stays on the agent computer and never in the database.
3. Are all device clocks set to Indian time?
4. For step 0503: what happens when an employee has only one punch in a day (missed out punch), and which shifts cross midnight?

## eSSL K30 Pro and K90 Pro (Owner, 2026-11-04)
Both are standalone terminals with ZKTeco-based firmware. They keep their punches in the device and normally answer on the LAN over the ZK protocol (TCP, port 4370, optional comm key), which is what the agent's `IPunchSource` reader would speak. They can also push punches to a server (ADMS) when pointed at one. The reader cannot be written safely without a device to test against. Decision needed: write the ZK-protocol reader and test it on one real device on your LAN, or have the devices push to the agent (the agent then listens instead of pulling, and nothing else changes in the module).

## Cleanup after the push of 2026-11-04
Origin holds two designs for recovery of overpaid claims. The `IArrearsRecoveryLedger` design (held at Process, released at Draft or Reopen) is the one wired into `PayrollRunService` and `PayrollModule`. Brave's earlier `IArrearsRecoveryStore` (applied at Lock) is no longer used, so its two classes and its tests are removed (they stopped the Payroll test project from compiling). `0337_pay_arrears_recovery_procs.sql` (the old procedures) is deleted: two migrations numbered 0337 made the repo guard fail, and nothing called its procedures. If it was applied to a database, the three procedures `usp_ArrearsRecovery_Pending`, `_Apply` and `_Reverse` can stay there unused or be dropped by hand.
