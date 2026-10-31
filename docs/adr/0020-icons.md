# ADR 0020: Icons

Status: accepted. Owner: Sigma.

- One icon font, Infinitude (SIL Open Font License per its name table), embedded unmodified in Revolution.UI.Common as `Icons/Infinitude.ttf`. No second font, no PNG or ICO sets for in-app icons.
- `AppIcon` is the only way to ask for an icon. `IconMap` is the only place with codepoints. `IconProvider` draws the glyph as a vector, scaled to fit, in the colour passed (from `Theme`), and caches it. Size is given in 96-dpi units plus the control's `DeviceDpi`, so icons stay sharp under PerMonitorV2.
- 94 members, listed in `docs/icons.md`. Some share one glyph on purpose (Close and Cancel, Backup, Restore, Preview and Database, Payslip and Receipt, Exit and Logout, Bank and BankFile, History and Clock, Password and Key, Address and Location). Call sites say what they mean, and a better glyph later changes one map line.
- No icon where the font has none. 30 planned icons were dropped: AuditTrail, Diagnostics, Attachment, Camera, Eye, EyeOff, Passport, Shift, HalfDay, Overtime, WeeklyOff, Earnings, ProfessionalTax, Loan, Emi, Deductions, Tds, Form16, Form24Q, TaxRegime, Arrears, FullAndFinal, Ecr, Challan, Rfid, PunchIn, PunchOut, AssetAssign, Expense, Claim. Those screens and buttons show text only. Nothing may draw a stand-in.
- Only Sigma adds members. Brave asks through a change request. A new icon needs a new font version from the font owner, then one enum member and one map line.
- `IconProviderTests` draws every member, so a member without a working glyph fails CI.

Font notes: glyph names in the font are not used (several are misspelled, and the padlock used for Lock is named `ugrave`). U+0234 to U+0236 are solid black blocks and are never mapped.

Open: Lock (U+00F9) and Unlock (U+00EB) look alike at small sizes, and Refresh (U+0828) is a loop with a tail. Check both on a running screen in the Phase gate screenshots.

## Window icon and menu icons (2026-10-29)

- Every form shows the application icon: the R mark, `AppIcon.AppLogo` (U+E003 of the icon font), in the accent colour. `BaseForm` sets it in its constructor from `AppWindowIcon`, built once with 8 sizes (16 to 256). A form never sets its own icon; the guard fails the build if one does.
- The exe icon (Explorer, taskbar pin) is `src/Revolution.App/Revolution.ico`, drawn from the same glyph. It is the only picture file allowed in the source. To change the mark, change the codepoint in `IconMap`, regenerate the .ico from it, and update this line.
- Menu positions get icons from `MenuIconMap` (Revolution.App), by entry key or group path. A position not listed shows text only. The File menu items are set in `ShellForm`.
- The Developer can check all of it on screen: Administration > Icon and menu map. Tab Screens lists every screen the feature modules offer (Designation, Daily attendance, the payroll exports and the rest), the menu positions that open it or "Not in the menu", and opens the selected one (double-click or the toolbar button) with the same View right the menu needs. Tab Menu positions lists every position with its icon, whether it has a screen or is planned, and the module and plan feature it needs. Tab Icons lists every `AppIcon` with its codepoint and which icons share a glyph.
