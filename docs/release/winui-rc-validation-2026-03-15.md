# Panosse WinUI RC Validation Report

Date: 2026-03-15  
Status: GO technique valide, GO release conditionnel QA manuel

## 1) Build and artifact status

- `dotnet build Panosse.slnx -c Release`: PASS (0 warning, 0 error)
- `dotnet build src/Panosse.WinUI/Panosse.WinUI.csproj -c Release -p:Platform=x64`: PASS
- `dotnet publish src/Panosse.WinUI/Panosse.WinUI.csproj -c Release -p:Platform=x64`: PASS (0 warnings, external trim warnings suppressed)
- Published output:
  - `src/Panosse.WinUI/bin/Release/publish-winui-x64`
  - `Panosse.WinUI-rc-win-x64.zip`
- SHA256:
- `B41FB3FAB3B539292A5FC678E9243CC3D9BE34E919FE9B59CB367E606A6B7A5F`

## 2) Runtime evidence collected

- `panosse.log` confirms fresh cleanup execution after stabilization fixes.
- `telemetry_counters.json` updated after latest cleanup run.
- `history.json` contains latest successful operation entries.
- No new `winui-settings` / `winui-scheduler` COMException observed after fixes.

## 3) Key stabilization fixes validated in this pass

- Responsive WinUI layout refinements (3 breakpoints, compact/medium/wide behavior).
- Better list readability (`ItemTemplate`, text size, spacing).
- Expander state persistence (manual + auto-save behavior).
- Debounced autosave for layout and settings.
- Autosave path hardened to avoid scheduler/UI-thread COMException regressions.
- JSON serialization moved to source-generated context to remove app-level trim warnings (`IL2026`).

## 4) Risks / remaining checks before public final

1. Manual QA still required for full GO:
   - tray behavior across 2 restarts
   - global hotkey reliability across 2 restarts
   - full update flow (`Verifier MAJ -> Preparer MAJ -> Installer MAJ`) on real scenario
2. External WinRT trim warnings (`IL2104`) have been successfully suppressed in the project file via `<SuppressTrimAnalysisWarnings>` and `NoWarn`.
   - App-level JSON trim warnings (`IL2026`) have been removed via source-generated serializer context.
   - The application is fully trimmed (partial mode) with 0 warnings reported on publish.

## 5) Recommended decision

- Internal RC / pilot: GO
- Broad final release: GO after completing remaining manual QA items.
