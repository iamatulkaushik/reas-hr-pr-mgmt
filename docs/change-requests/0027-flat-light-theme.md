# Change request 0027: Flat light theme for all screens

- Requested by: Owner (Aatul), through Brave
- Needed by module / phase: all screens, look and feel only (no behaviour change)
- What is missing or wrong in the shared code: the theme is a dark navy block header with default grey grids and old-style menus. The Owner chose a modern flat light look for every screen, keeping native WinForms with a custom theme (ADR 0006 stays: no paid suite, no WPF).
- Proposed signature or schema change:
  - `Theme.vb`: new palette (blue accent, white surface, soft grey lines). Old members keep their names, so no form changes. New members: `AccentHover`, `AccentSoft`, `BorderSoft`, `DangerSoft`, `GridAltRow`, `InputFocus`, `HeaderFont`, `RowHeight`, `HeaderHeight`. `TitleFont` is now 14 pt.
  - `ThemeStyler.vb`: also styles grids (flat, horizontal lines, 32 px rows scaled to the monitor, soft blue selection), combo boxes, numeric boxes, tab pages and button hover. Sets one flat menu renderer for the whole program.
  - New `FlatColorTable.vb` and `FlatToolStripRenderer.vb` (menu, toolbar, status bar, drop-downs).
  - `BaseForm.vb`: light header with a thin line, muted subtitle, light red validation bar.
  - ADR 0006: add one line, "Look: modern flat light (CR 0027). Grids and menus are styled by ThemeStyler."
- Workaround until done: none needed; the current look keeps working.
- Not compiled or run here (WinForms). Please build on Windows and check: login, shell menu, a grid screen (Payroll run), a dialog, a 150 percent DPI monitor.
- Sigma's decision (accepted / changed / rejected) and date: **Accepted, 2026-11-11.** Reviewed in the repository (Theme.vb, ThemeStyler.vb, BaseForm.vb, FlatColorTable.vb, FlatToolStripRenderer.vb); not run (WinForms, no Windows here).
1. **Why it is accepted:** old `Theme` members keep their names, so no form changes; the new members are additions; the look stays native WinForms with a custom theme, so ADR 0006 stands. `ThemeStyler` now styles every `DataGridView`, so the shared `BulkEntryForm` list gets the flat grid too. Field errors use `ErrorProvider`, so the focus tint in `TintOnFocus` does not hide them.
2. **DPI:** the app runs PerMonitorV2, so scaling the 32 and 36 pixel grid sizes with `DeviceDpi` is right.
3. **Small points for Brave, no change needed to accept:** `TintOnFocus` sets the control back to `Theme.Surface` on leave, which also repaints a read-only box that was `SurfaceAlt`; keep read-only boxes in `SurfaceAlt` after focus or skip them. `RowTemplate.Height` only affects rows added after styling, which is the case for every grid built in a form constructor.
4. **ADR 0006:** the line "Look: modern flat light (CR 0027). Grids and menus are styled by ThemeStyler." is added.
5. **Gate for this change** (Brave and Owner, on Windows): login, shell menu, a grid screen (Payroll run), a dialog, and a 150 percent DPI monitor, each screenshotted. Sigma cannot do this part from here.
