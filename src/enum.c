#include "enum.h"
#include "util.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

// Monitor enumeration
MonitorList* enumerate_monitors() {
    MonitorList* list = (MonitorList*)malloc(sizeof(MonitorList));
    if (!list) return NULL;

    list->monitors = NULL;
    list->count = 0;

    // Enumerate all display devices
    DISPLAY_DEVICEA display_device;
    display_device.cb = sizeof(DISPLAY_DEVICEA);

    int monitor_index = 1; // Start with M1, M2, etc.

    for (DWORD device_index = 0;; device_index++) {
        if (!EnumDisplayDevicesA(NULL, device_index, &display_device, 0)) {
            break; // No more devices
        }

        // Skip devices that are not active
        if (!(display_device.StateFlags & DISPLAY_DEVICE_ACTIVE)) {
            continue;
        }

        // Skip mirroring drivers
        if (display_device.StateFlags & DISPLAY_DEVICE_MIRRORING_DRIVER) {
            continue;
        }

        // Get current display settings for this device
        DEVMODEA devmode;
        memset(&devmode, 0, sizeof(DEVMODEA));
        devmode.dmSize = sizeof(DEVMODEA);

        if (!EnumDisplaySettingsExA(display_device.DeviceName, ENUM_CURRENT_SETTINGS, &devmode, 0)) {
            log_verbose("Failed to get display settings for device: %s", display_device.DeviceName);
            continue;
        }

        // Create monitor info
        MonitorInfo* monitor = (MonitorInfo*)malloc(sizeof(MonitorInfo));
        if (!monitor) {
            log_error("Failed to allocate memory for monitor info");
            continue;
        }

        // Generate monitor ID (M1, M2, etc.)
        size_t id_len = snprintf(NULL, 0, "M%d", monitor_index) + 1;
        monitor->id = (char*)malloc(id_len);
        if (monitor->id) {
            sprintf_s(monitor->id, id_len, "M%d", monitor_index);
        }

        // Copy device information
        monitor->device_name = _strdup(display_device.DeviceString);
        monitor->device_path = _strdup(display_device.DeviceName);
        monitor->device_id = _strdup(display_device.DeviceID);
        monitor->width = devmode.dmPelsWidth;
        monitor->height = devmode.dmPelsHeight;
        monitor->orientation = devmode.dmDisplayOrientation;

        if (!monitor->device_name || !monitor->device_path || !monitor->device_id) {
            free(monitor->id);
            free(monitor->device_name);
            free(monitor->device_path);
            free(monitor->device_id);
            free(monitor);
            continue;
        }

        // Add to list
        MonitorInfo* new_monitors = (MonitorInfo*)realloc(list->monitors, (list->count + 1) * sizeof(MonitorInfo));
        if (!new_monitors) {
            free(monitor->id);
            free(monitor->device_name);
            free(monitor->device_path);
            free(monitor->device_id);
            free(monitor);
            continue;
        }

        list->monitors = new_monitors;
        list->monitors[list->count] = *monitor;
        free(monitor); // Free the temporary struct, keep the contents
        list->count++;
        monitor_index++;

        log_verbose("Enumerated monitor: ID=%s, Name='%s', Path='%s', Resolution=%dx%d, Orientation=%d",
                   list->monitors[list->count - 1].id,
                   list->monitors[list->count - 1].device_name,
                   list->monitors[list->count - 1].device_path,
                   list->monitors[list->count - 1].width,
                   list->monitors[list->count - 1].height,
                   list->monitors[list->count - 1].orientation);
    }

    return list;
}

void free_monitor_list(MonitorList* list) {
    if (!list) return;

    for (int i = 0; i < list->count; i++) {
        free(list->monitors[i].id);
        free(list->monitors[i].device_name);
        free(list->monitors[i].device_path);
        free(list->monitors[i].device_id);
    }
    free(list->monitors);
    free(list);
}

// Monitor listing and formatting
void print_monitor_table(const MonitorList* monitors) {
    if (!monitors || monitors->count == 0) {
        printf("No monitors found.\n");
        return;
    }

    // Print header
    printf("%-5s %-30s %-20s %-15s %-12s\n", "ID", "Name", "Device", "Resolution", "Rotation");
    printf("%-5s %-30s %-20s %-15s %-12s\n", "----", "----", "------", "----------", "--------");

    // Print each monitor
    for (int i = 0; i < monitors->count; i++) {
        const MonitorInfo* monitor = &monitors->monitors[i];

        // Truncate device path if too long
        char device_path_short[21];
        if (strlen(monitor->device_path) > 20) {
            memcpy(device_path_short, monitor->device_path, 17);
            strcpy(device_path_short + 17, "...");
        } else {
            strcpy_s(device_path_short, sizeof(device_path_short), monitor->device_path);
        }

        printf("%-5s %-30s %-20s %-15s %-12s\n",
               monitor->id,
               monitor->device_name,
               device_path_short,
               get_resolution_string(monitor->width, monitor->height),
               get_orientation_string(monitor->orientation));
    }
}

char* get_orientation_string(DWORD orientation) {
    switch (orientation) {
        case DMDO_DEFAULT: return "0°";
        case DMDO_90:      return "90°";
        case DMDO_180:     return "180°";
        case DMDO_270:     return "270°";
        default: {
            static char buffer[16];
            sprintf_s(buffer, sizeof(buffer), "%lu°", orientation);
            return buffer;
        }
    }
}

char* get_resolution_string(DWORD width, DWORD height) {
    static char buffer[32];
    sprintf_s(buffer, sizeof(buffer), "%lux%lu", width, height);
    return buffer;
}

// Monitor finding utilities
MonitorInfo* find_monitor_by_id(const MonitorList* monitors, const char* id) {
    if (!monitors || !id) return NULL;

    for (int i = 0; i < monitors->count; i++) {
        if (strcmp(monitors->monitors[i].id, id) == 0) {
            return &monitors->monitors[i];
        }
    }
    return NULL;
}

MonitorInfo* find_monitor_by_device_path(const MonitorList* monitors, const char* device_path) {
    if (!monitors || !device_path) return NULL;

    for (int i = 0; i < monitors->count; i++) {
        if (strcmp(monitors->monitors[i].device_path, device_path) == 0) {
            return &monitors->monitors[i];
        }
    }
    return NULL;
}

int get_monitor_index(const MonitorList* monitors, const MonitorInfo* monitor) {
    if (!monitors || !monitor) return -1;

    for (int i = 0; i < monitors->count; i++) {
        if (&monitors->monitors[i] == monitor) {
            return i;
        }
    }
    return -1;
}
