#include "rotate.h"
#include "util.h"
#include "enum.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

// Single monitor rotation
RotationResult rotate_monitor(const MonitorInfo* monitor, RotationCommand command, bool dry_run) {
    RotationResult result = { false, 0, 0, 0 };

    if (!monitor) {
        result.error_code = DISP_CHANGE_BADPARAM;
        return result;
    }

    // Get current display settings
    DEVMODEA devmode;
    memset(&devmode, 0, sizeof(DEVMODEA));
    devmode.dmSize = sizeof(DEVMODEA);

    if (!EnumDisplaySettingsExA(monitor->device_path, ENUM_CURRENT_SETTINGS, &devmode, 0)) {
        result.error_code = DISP_CHANGE_BADMODE;
        log_verbose("Failed to get current display settings for %s", monitor->device_path);
        return result;
    }

    result.old_orientation = devmode.dmDisplayOrientation;
    result.new_orientation = get_target_orientation(devmode.dmDisplayOrientation, command);

    log_verbose("Rotating monitor %s (%s) from %s to %s",
               monitor->id, monitor->device_path,
               get_orientation_string(result.old_orientation),
               get_orientation_string(result.new_orientation));

    // Prepare new display settings
    DEVMODEA new_devmode = devmode;
    new_devmode.dmDisplayOrientation = result.new_orientation;
    new_devmode.dmFields |= DM_DISPLAYORIENTATION;

    // Swap dimensions if rotating between landscape and portrait
    if (should_swap_dimensions(result.old_orientation, result.new_orientation)) {
        DWORD temp = new_devmode.dmPelsWidth;
        new_devmode.dmPelsWidth = new_devmode.dmPelsHeight;
        new_devmode.dmPelsHeight = temp;
        new_devmode.dmFields |= (DM_PELSWIDTH | DM_PELSHEIGHT);

        log_verbose("Swapping dimensions: %lux%lu -> %lux%lu",
                   devmode.dmPelsWidth, devmode.dmPelsHeight,
                   new_devmode.dmPelsWidth, new_devmode.dmPelsHeight);
    }

    if (dry_run) {
        log_info("[DRY RUN] Would rotate %s from %s to %s", monitor->id,
                get_orientation_string(result.old_orientation),
                get_orientation_string(result.new_orientation));
        result.success = true;
        return result;
    }

    // Apply the display settings change
    LONG change_result = ChangeDisplaySettingsExA(monitor->device_path, &new_devmode, NULL,
                                                 CDS_UPDATEREGISTRY | CDS_GLOBAL, NULL);

    result.error_code = change_result;
    result.success = (change_result == DISP_CHANGE_SUCCESSFUL);

    if (result.success) {
        log_verbose("Successfully rotated monitor %s", monitor->id);
    } else {
        log_error("Failed to rotate monitor %s: error code %ld", monitor->id, change_result);
        switch (change_result) {
            case DISP_CHANGE_BADDUALVIEW:
                log_error("The settings change was unsuccessful because the system is DualView capable.");
                break;
            case DISP_CHANGE_BADFLAGS:
                log_error("An invalid set of flags was passed in.");
                break;
            case DISP_CHANGE_BADMODE:
                log_error("The graphics mode is not supported.");
                break;
            case DISP_CHANGE_BADPARAM:
                log_error("An invalid parameter was passed in.");
                break;
            case DISP_CHANGE_FAILED:
                log_error("The display driver failed the specified graphics mode.");
                break;
            case DISP_CHANGE_NOTUPDATED:
                log_error("Unable to write settings to the registry.");
                break;
            case DISP_CHANGE_RESTART:
                log_error("The computer must be restarted for the graphics mode to work.");
                break;
            default:
                log_error("Unknown error occurred.");
                break;
        }
    }

    return result;
}

// Batch rotation with selector filtering
BatchRotationResult rotate_monitors_filtered(const MonitorList* monitors,
                                           RotationCommand command,
                                           const SelectorList* include_selectors,
                                           const SelectorList* exclude_selectors,
                                           bool dry_run) {
    BatchRotationResult batch_result = { 0, 0, NULL };

    if (!monitors || monitors->count == 0) {
        return batch_result;
    }

    // Allocate results array
    batch_result.results = (RotationResult*)malloc(monitors->count * sizeof(RotationResult));
    if (!batch_result.results) {
        log_error("Failed to allocate memory for rotation results");
        return batch_result;
    }

    for (int i = 0; i < monitors->count; i++) {
        const MonitorInfo* monitor = &monitors->monitors[i];
        bool should_process = true;

        // Check include selectors (if specified)
        if (include_selectors && include_selectors->count > 0) {
            should_process = false;
            for (int j = 0; j < include_selectors->count; j++) {
                if (matches_monitor(&include_selectors->selectors[j],
                                  monitor->id, monitor->device_path, monitor->device_name)) {
                    should_process = true;
                    break;
                }
            }
        }

        // Check exclude selectors
        if (exclude_selectors && exclude_selectors->count > 0) {
            for (int j = 0; j < exclude_selectors->count; j++) {
                if (matches_monitor(&exclude_selectors->selectors[j],
                                  monitor->id, monitor->device_path, monitor->device_name)) {
                    should_process = false;
                    break;
                }
            }
        }

        if (should_process) {
            batch_result.results[i] = rotate_monitor(monitor, command, dry_run);
            if (batch_result.results[i].success) {
                batch_result.success_count++;
            } else {
                batch_result.failure_count++;
            }
        } else {
            // Monitor was filtered out, mark as successful (no-op)
            batch_result.results[i].success = true;
            batch_result.results[i].error_code = DISP_CHANGE_SUCCESSFUL;
            batch_result.results[i].old_orientation = monitor->orientation;
            batch_result.results[i].new_orientation = monitor->orientation;
        }
    }

    return batch_result;
}

// Rollback functionality
RollbackState* create_rollback_state(const MonitorList* monitors) {
    if (!monitors || monitors->count == 0) return NULL;

    RollbackState* state = (RollbackState*)malloc(sizeof(RollbackState));
    if (!state) return NULL;

    state->infos = (RollbackInfo*)malloc(monitors->count * sizeof(RollbackInfo));
    if (!state->infos) {
        free(state);
        return NULL;
    }

    state->count = 0;

    for (int i = 0; i < monitors->count; i++) {
        const MonitorInfo* monitor = &monitors->monitors[i];

        // Get current display settings
        DEVMODEA devmode;
        memset(&devmode, 0, sizeof(DEVMODEA));
        devmode.dmSize = sizeof(DEVMODEA);

        if (!EnumDisplaySettingsExA(monitor->device_path, ENUM_CURRENT_SETTINGS, &devmode, 0)) {
            log_verbose("Failed to get current settings for rollback: %s", monitor->device_path);
            continue;
        }

        RollbackInfo* info = &state->infos[state->count];
        info->device_path = _strdup(monitor->device_path);
        info->original_orientation = devmode.dmDisplayOrientation;
        info->original_width = devmode.dmPelsWidth;
        info->original_height = devmode.dmPelsHeight;

        if (!info->device_path) {
            continue;
        }

        state->count++;
    }

    if (state->count == 0) {
        free_rollback_state(state);
        return NULL;
    }

    return state;
}

bool rollback_monitors(const RollbackState* rollback_state, bool dry_run) {
    if (!rollback_state || rollback_state->count == 0) return true;

    bool all_successful = true;

    for (int i = 0; i < rollback_state->count; i++) {
        const RollbackInfo* info = &rollback_state->infos[i];

        // Get current display settings
        DEVMODEA devmode;
        memset(&devmode, 0, sizeof(DEVMODEA));
        devmode.dmSize = sizeof(DEVMODEA);

        if (!EnumDisplaySettingsExA(info->device_path, ENUM_CURRENT_SETTINGS, &devmode, 0)) {
            log_error("Failed to get current settings for rollback of %s", info->device_path);
            all_successful = false;
            continue;
        }

        log_verbose("Rolling back %s from %s to %s",
                   info->device_path,
                   get_orientation_string(devmode.dmDisplayOrientation),
                   get_orientation_string(info->original_orientation));

        if (dry_run) {
            log_info("[DRY RUN] Would rollback %s to %s", info->device_path,
                    get_orientation_string(info->original_orientation));
            continue;
        }

        // Prepare rollback settings
        DEVMODEA rollback_devmode = devmode;
        rollback_devmode.dmDisplayOrientation = info->original_orientation;
        rollback_devmode.dmPelsWidth = info->original_width;
        rollback_devmode.dmPelsHeight = info->original_height;
        rollback_devmode.dmFields = DM_DISPLAYORIENTATION | DM_PELSWIDTH | DM_PELSHEIGHT;

        LONG result = ChangeDisplaySettingsExA(info->device_path, &rollback_devmode, NULL,
                                              CDS_UPDATEREGISTRY | CDS_GLOBAL, NULL);

        if (result != DISP_CHANGE_SUCCESSFUL) {
            log_error("Failed to rollback monitor %s: error %ld", info->device_path, result);
            all_successful = false;
        } else {
            log_verbose("Successfully rolled back monitor %s", info->device_path);
        }
    }

    return all_successful;
}

void free_rollback_state(RollbackState* state) {
    if (!state) return;

    for (int i = 0; i < state->count; i++) {
        free(state->infos[i].device_path);
    }
    free(state->infos);
    free(state);
}

// Orientation utilities
DWORD get_target_orientation(DWORD current_orientation, RotationCommand command) {
    switch (command) {
        case ROTATION_LANDSCAPE:
            return DMDO_DEFAULT; // 0°

        case ROTATION_PORTRAIT:
            return DMDO_90; // 90°

        case ROTATION_TOGGLE:
            // Toggle between 0° and 90°
            return (current_orientation == DMDO_DEFAULT) ? DMDO_90 : DMDO_DEFAULT;

        default:
            return current_orientation;
    }
}

bool should_swap_dimensions(DWORD from_orientation, DWORD to_orientation) {
    // Check if we're rotating between landscape and portrait orientations
    bool from_is_portrait = (from_orientation == DMDO_90 || from_orientation == DMDO_270);
    bool to_is_portrait = (to_orientation == DMDO_90 || to_orientation == DMDO_270);

    return (from_is_portrait != to_is_portrait);
}
