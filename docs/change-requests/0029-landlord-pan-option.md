# Change request 0029: landlord PAN option on the rent row (Form 124 and TDS calculation)

- Requested by: Aatul (owner direction, 2026-11-14): give an option while submitting income tax documents, and carry it into the calculation.
- Needed by module / phase: Phase 6 (TDS declaration, projection). Backend and migration by Brave. UI column by Sigma.
- What is missing or wrong in the shared code: `TdsDeclarationValidator.CheckLandlordPan` blocks the save with one message when the yearly rent to one landlord is above 1,00,000 and no PAN is typed. The associate has no way to record "landlord has no PAN" or to continue without claiming HRA. Form 124 guidance: the PAN is required above 1,00,000 a year; without a PAN or a valid no-PAN declaration the employer disallows the HRA exemption in TDS. The owner said the PAN is not needed in Haryana; the guidance has no state exception, so the PAN rule itself stays. This CR adds a lawful way through it, not a waiver.
- Proposed signature or schema change:
  - `TdsLandlordPanOption` (Byte enum): `PanGiven` 0, `NoPanDeclared` 1, `ExemptionNotClaimed` 2.
  - `TdsRentInput` and `TdsRentView` take an optional last argument `panOption` (default `PanGiven`), so no existing call breaks. `TdsRentView.ExemptionAllowed` is False for option 2.
  - Migration 0343: `pay.TdsHraRent.PanOption TINYINT NOT NULL DEFAULT 0`, check `PanOption IN (0,1,2)` and `PanOption = 0` when a PAN is stored. `usp_TdsHraRent_List` returns it; `usp_TdsHraRent_Replace` reads JSON `PanOption` (optional).
  - Validator: above the threshold with no PAN, option 0 fails with a message naming the three choices; option 1 needs the landlord address; option 2 passes; a row with a PAN must use option 0; an unknown value fails.
  - Calculation: `TdsProjectionService.RentFor` ignores rows with option 2, so no HRA exemption is given for those months. Option 1 gives the exemption as before.
- Screen text for Sigma (rent grid, new column "Landlord PAN"): `Landlord PAN entered` / `Landlord has no PAN (written declaration held)` / `Do not claim HRA exemption`. Pass the choice as the ninth argument of `TdsRentInput`. Show the third choice with a note: "No HRA exemption will be given for these months." The first choice stays the default.
- Workaround until done: type a PAN, or leave the rent row out.
- Not done / open:
  - Not compiled on Windows and not run against SQL Server (no dotnet here). Eight tests added (6 validator, 2 projection); run them first.
  - The 1,00,000 threshold is still a code constant until CR 0018's `LANDLORD_PAN_RENT` row is read by the validator.
  - CA to confirm what the no-PAN declaration must hold (Form 124 supporting documents) and whether the app should store a reference to it.
  - Owner to confirm the PAN rule applies in Haryana.
- Sigma's decision (accepted / changed / rejected) and date:
