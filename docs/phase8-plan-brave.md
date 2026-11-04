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
| 0502 | `Revolution.Agent` (Windows Service) and `Revolution.Agent.Essl` (the eSSL reader behind `IPunchSource`) | waiting for the eSSL model and protocol |
| 0503 | Attendance from punches: first in and last out per employee and day, shift and night shift rules, into `att.AttendanceDaily` | not started; needs the Owner's rules for a missed punch and a night shift |
| 0504 | Plain screens: devices, card mapping, punch list with unmatched punches | not started |

## Rules built into step 0500
A punch is stored once: the same device, time, device user and card is ignored, so the agent may pull again from a few minutes before the newest stored punch (five minutes) and nothing doubles. A punch is matched to an employee when it arrives, by the active card first and then by `BiometricID`; it stays unmatched until `usp_Punch_Resolve` is run after a card is mapped or an id is set. A card on another active employee is refused; a new card for an employee retires the old one. A punch time more than a day ahead or more than three years back is thrown away and counted (a device that lost its clock reports 2000-01-01). Times are the device's own clock, the factory's local time.

## Questions for the Owner
1. Which eSSL model, and how does the company use it today: the eSSL software with its own database, or the devices alone on the LAN? The reader reads over the network (the ZK protocol on port 4370 is what most eSSL devices speak); if the devices only push to eSSL's own software, the reader reads that software's log table instead.
2. Do the devices have a comm key (password)? It stays on the agent computer and never in the database.
3. Are all device clocks set to Indian time?
4. For step 0503: what happens when an employee has only one punch in a day (missed out punch), and which shifts cross midnight?
