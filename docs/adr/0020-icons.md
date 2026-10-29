# ADR 0020: Icons

Status: accepted. Owner: Sigma.

- One icon font, Infinitude (SIL Open Font License per its name table), embedded unmodified in Revolution.UI.Common as `Icons/Infinitude.ttf`. No second font, no PNG or ICO sets for in-app icons.
- `AppIcon` is the only way to ask for an icon. `IconMap` is the only place with codepoints. `IconProvider` draws the glyph as a vector, scaled to fit, in the colour passed (from `Theme`), and caches it. Size is given in 96-dpi units plus the control's `DeviceDpi`, so icons stay sharp under PerMonitorV2.
- 93 members, listed in `docs/icons.md`. Some share one glyph on purpose (Close and Cancel, Backup, Restore, Preview and Database, Payslip and Receipt, Exit and Logout, Bank and BankFile, History and Clock, Password and Key, Address and Location). Call sites say what they mean, and a better glyph later changes one map line.
- No icon where the font has none. 30 planned icons were dropped: AuditTrail, Diagnostics, Attachment, Camera, Eye, EyeOff, Passport, Shift, HalfDay, Overtime, WeeklyOff, Earnings, ProfessionalTax, Loan, Emi, Deductions, Tds, Form16, Form24Q, TaxRegime, Arrears, FullAndFinal, Ecr, Challan, Rfid, PunchIn, PunchOut, AssetAssign, Expense, Claim. Those screens and buttons show text only. Nothing may draw a stand-in.
- Only Sigma adds members. Brave asks through a change request. A new icon needs a new font version from the font owner, then one enum member and one map line.
- `IconProviderTests` draws every member, so a member without a working glyph fails CI.

Font notes: glyph names in the font are not used (several are misspelled, and the padlock used for Lock is named `ugrave`). U+0234 to U+0236 are solid black blocks and are never mapped.

Open: Lock (U+00F9) and Unlock (U+00EB) look alike at small sizes, and Refresh (U+0828) is a loop with a tail. Check both on a running screen in the Phase gate screenshots.
