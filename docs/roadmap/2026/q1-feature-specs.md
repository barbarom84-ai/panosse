# Q1 2026 Feature Specs (Jan-Mar)

This document defines the implementation scope and acceptance criteria for the first 3 roadmap features.

## January - Cleanup Preview Mode

### Goal
Allow users to run a scan-only cleanup preview to estimate recoverable space by category before deleting files.

### Scope
- Add a new user action: `Apercu du nettoyage`.
- Reuse existing cleanup categories (Temp, caches, downloads, logs, thumbnails, etc.).
- Compute size estimates without deleting files.
- Show per-category estimate and total estimate in UI.
- Add explicit confirmation before launching real cleanup.

### Technical Targets
- Service layer:
  - Create a preview model with category name, estimated bytes, and optional warnings.
  - Add scan-only methods in cleanup service (or dedicated preview service).
- ViewModel:
  - Add preview state: loading, result list, total estimate, confirmation visibility.
- UI:
  - Add preview button and preview panel/modal.

### Acceptance Criteria
- Running preview never deletes files or changes registry values.
- Preview completes successfully even when some folders are inaccessible (partial results allowed).
- Total estimate and per-category estimates are displayed.
- User can start cleanup from preview confirmation in one action.
- App remains responsive during preview execution.

## February - Smart Exclusions

### Goal
Let users exclude paths and extensions from cleanup to reduce accidental deletion risk.

### Scope
- Add exclusion settings:
  - Folder path exclusions.
  - Extension exclusions.
- Apply exclusions to cleanup and preview.
- Add safe defaults that can be edited.

### Technical Targets
- Settings:
  - Extend settings model with exclusion lists.
  - Persist exclusions in settings JSON.
- Cleanup pipeline:
  - Centralize exclusion checks so all file-based steps honor them.
- UI:
  - Settings panel for listing/adding/removing exclusions.

### Acceptance Criteria
- Excluded files/folders are never deleted by cleanup.
- Exclusions are honored in preview estimates.
- Invalid or inaccessible exclusion paths do not crash cleanup.
- Exclusions persist across app restart.
- User can reset exclusions to defaults.

## March - Scheduled Cleanup

### Goal
Provide automatic cleanup scheduling (daily/weekly) with clear status and user control.

### Scope
- Add schedule configuration:
  - Enabled/disabled.
  - Frequency: daily or weekly.
  - Preferred time.
- Run cleanup silently according to schedule.
- Surface last scheduled run result in UI/history.

### Technical Targets
- Scheduler service:
  - Store schedule settings.
  - Trigger cleanup at expected time.
  - Prevent overlapping runs.
- Telemetry/history:
  - Record scheduled trigger and run outcome.
- UI:
  - Settings controls for schedule and status summary.

### Acceptance Criteria
- Scheduled cleanup starts automatically when enabled.
- Disabled schedule never triggers cleanup.
- Missed run behavior is deterministic (run once after app starts or skip by design).
- Scheduled cleanup uses same safety rules as manual cleanup.
- Last run timestamp and result are visible to the user.

