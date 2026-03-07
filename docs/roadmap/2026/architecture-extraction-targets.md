# 2026 Architecture Extraction Targets

This document defines what to extract from `MainWindow` before/alongside each monthly feature.

## Guiding Rules
- Keep UI state in ViewModels.
- Move file/network/system operations to Services.
- Keep code-behind focused on wiring, visual transitions, and window events.

## Monthly Extraction Plan

### January - Cleanup Preview Mode
- From code-behind to Services:
  - Extract preview scan logic into `CleanupPreviewService`.
  - Share category definitions with `CleanupService`.
- From code-behind to ViewModel:
  - Preview state (`isLoading`, list items, total bytes, error text).

### February - Smart Exclusions
- From code-behind to Services:
  - Add `ExclusionService` and centralized `ShouldExclude(path, extension)`.
- From code-behind to ViewModel:
  - Exclusion list collections and validation state.

### March - Scheduled Cleanup
- From code-behind to Services:
  - Add `CleanupSchedulerService` (schedule evaluation, next run computation, trigger).
- From code-behind to ViewModel:
  - Schedule configuration fields and status summary.

### April - Cleanup History Timeline
- From code-behind to Services:
  - Add `CleanupHistoryService` for reading/writing run records.
- From code-behind to ViewModel:
  - History list, filters, selected run detail.

### May - Browser Cleanup Expansion
- From code-behind to Services:
  - Browser-specific cleaners behind an interface (`IBrowserCleanupProvider`).
- From code-behind to ViewModel:
  - Per-browser enable/disable flags.

### June - Deep Clean Profile
- From code-behind to Services:
  - Add `CleanupProfileService` for profile resolution and risk tags.
- From code-behind to ViewModel:
  - Profile picker and pre-run warning panel state.

### July - Performance Pass
- From code-behind to Services:
  - Introduce cancellable pipeline orchestration (`CleanupOrchestratorService`).
- From code-behind to ViewModel:
  - Cancellation state, finer progress model.

### August - Diagnostics Center
- From code-behind to Services:
  - Add `DiagnosticsService` with checks (permissions, lock contention, path access).
- From code-behind to ViewModel:
  - Diagnostics result cards and severity summaries.

### September - Settings v2
- From code-behind to Services:
  - Evolve settings into versioned configuration with migration support.
- From code-behind to ViewModel:
  - Dedicated settings sections/tabs and validation state.

### October - Auto-Update v2
- From code-behind to Services:
  - Split updater into `UpdateCheckService`, `UpdateDownloadService`, `UpdateInstallService`.
- From code-behind to ViewModel:
  - Update state machine (idle/checking/downloading/installing/error).

### November - Accessibility and UI Polish
- From code-behind to Services:
  - Optional `AccessibilityService` for persisted accessibility preferences.
- From code-behind to ViewModel:
  - UI scale and accessibility preference state.

### December - Reliability and LTS
- From code-behind to Services:
  - Finalize service boundaries and remove legacy direct logic calls.
- From code-behind to ViewModel:
  - Consolidate shared UI state helpers and command guards.

## Target End-State (Dec 2026)
- `MainWindow.xaml.cs` only coordinates:
  - Window lifecycle, overlays animations, dispatcher-safe UI glue.
- Services own:
  - Cleanup, preview, exclusion rules, scheduler, history, diagnostics, updates.
- ViewModels own:
  - User-visible state, command availability, validation messages.

