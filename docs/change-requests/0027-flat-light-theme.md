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
- Sigma's decision (accepted / changed / rejected) and date:
