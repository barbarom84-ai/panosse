# Monthly Release Checklist

Use this checklist for every monthly roadmap release.

## 1) Scope and Version
- [ ] Feature scope is frozen for the month.
- [ ] Version is updated (`src/Panosse.WinUI/Panosse.WinUI.csproj`, release notes, installer metadata).
- [ ] Changelog entry drafted with user-facing summary and known limitations.

## 2) Build Validation
- [ ] Debug build succeeds: `dotnet build`
- [ ] Release build succeeds: `dotnet build -c Release`
- [ ] Publish output generated and verified (size and required files).

## 3) Smoke Test (Manual)
- [ ] App launches with admin elevation as expected.
- [ ] Single-instance behavior works (second launch blocked with message).
- [ ] Cleanup button runs full pipeline and final status is shown.
- [ ] Tray behavior works (minimize/reopen/quit).
- [ ] About panel opens and shows expected metadata.
- [ ] Settings panel opens/saves/reloads values correctly.

## 4) Update Test
- [ ] Check-update action completes in normal network conditions.
- [ ] Offline/timeout path handled without crash.
- [ ] Update download/install fallback behavior verified.

## 5) Installer Test
- [ ] Installer builds successfully.
- [ ] Fresh install test passes on clean environment.
- [ ] Upgrade over previous version works.
- [ ] Uninstall path removes app correctly.

## 6) Reliability and Quality Gates
- [ ] No new lint warnings/errors on changed files.
- [ ] No regressions in critical cleanup steps.
- [ ] Error handling verified on locked files/access denied cases.

## 7) Publish
- [ ] Tag and release notes finalized.
- [ ] GitHub release artifacts uploaded (EXE/installer as applicable).
- [ ] Post-release sanity check done from release artifacts.

## 8) Post-Release Follow-up
- [ ] Monitor crash/error logs for 48h.
- [ ] Capture user feedback for next month planning.
- [ ] Update roadmap status and next month implementation brief.

