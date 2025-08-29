#ifndef ROTATE_H
#define ROTATE_H

#include "enum.h"
#include "util.h"
#include <windows.h>

// Rotation commands
typedef enum {
    ROTATION_LANDSCAPE,    // Set to 0°
    ROTATION_PORTRAIT,     // Set to 90°
    ROTATION_TOGGLE        // Toggle between 0° and 90°
} RotationCommand;

// Rotation result
typedef struct {
    bool success;
    LONG error_code;
    DWORD old_orientation;
    DWORD new_orientation;
} RotationResult;

// Single monitor rotation
RotationResult rotate_monitor(const MonitorInfo* monitor, RotationCommand command, bool dry_run);

// Batch rotation with selector filtering
typedef struct {
    int success_count;
    int failure_count;
    RotationResult* results;
} BatchRotationResult;

BatchRotationResult rotate_monitors_filtered(const MonitorList* monitors,
                                           RotationCommand command,
                                           const SelectorList* include_selectors,
                                           const SelectorList* exclude_selectors,
                                           bool dry_run);

// Rollback functionality
typedef struct {
    char* device_path;
    DWORD original_orientation;
    DWORD original_width;
    DWORD original_height;
} RollbackInfo;

typedef struct {
    RollbackInfo* infos;
    int count;
} RollbackState;

RollbackState* create_rollback_state(const MonitorList* monitors);
bool rollback_monitors(const RollbackState* rollback_state, bool dry_run);
void free_rollback_state(RollbackState* state);

// Orientation utilities
DWORD get_target_orientation(DWORD current_orientation, RotationCommand command);
bool should_swap_dimensions(DWORD from_orientation, DWORD to_orientation);

#endif // ROTATE_H
