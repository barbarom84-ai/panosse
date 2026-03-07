# Lightweight Telemetry Counters

Panosse records local aggregate counters to guide roadmap decisions without external analytics.

## Storage
- File: `%APPDATA%\\Panosse\\telemetry_counters.json`
- Format:
  - `counters`: key/value long integers
  - `lastUpdatedUtc`: UTC timestamp

## Current Counters
- `app_launch_count`
- `settings_open_count`
- `settings_save_count`
- `about_open_count`
- `cleanup_manual_start_count`
- `cleanup_manual_success_count`
- `cleanup_manual_freed_mb_total`
- `cleanup_background_start_count`
- `cleanup_background_success_count`
- `cleanup_background_failed_count`
- `update_check_start_count`
- `update_check_failed_count`
- `update_check_up_to_date_count`
- `update_check_update_available_count`
- `update_check_error_count`
- `update_install_start_count`
- `update_install_success_count`
- `update_install_failed_count`
- `tray_notification_shown_count`

## Usage
- Product decisions:
  - Compare manual vs background cleanup usage.
  - Track update availability and install success rates.
  - Validate settings adoption trends.
- Reliability:
  - Monitor failed background cleanups and update errors.

## Privacy Model
- Counters stay local on the device.
- No remote transmission is implemented.
- No user identifiers are stored.

