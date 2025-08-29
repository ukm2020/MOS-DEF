#ifndef ENUM_H
#define ENUM_H

#include <windows.h>
#include <stdbool.h>

// Monitor information structure
typedef struct {
    char* id;           // M1, M2, etc.
    char* device_name;  // DeviceString from DISPLAY_DEVICE
    char* device_path;  // DeviceName from DISPLAY_DEVICE (\\.\DISPLAYn)
    DWORD width;
    DWORD height;
    DWORD orientation;  // 0, 90, 180, 270
    char* device_id;    // DeviceID from DISPLAY_DEVICE
} MonitorInfo;

typedef struct {
    MonitorInfo* monitors;
    int count;
} MonitorList;

// Monitor enumeration
MonitorList* enumerate_monitors();
void free_monitor_list(MonitorList* list);

// Monitor listing and formatting
void print_monitor_table(const MonitorList* monitors);
char* get_orientation_string(DWORD orientation);
char* get_resolution_string(DWORD width, DWORD height);

// Monitor finding utilities
MonitorInfo* find_monitor_by_id(const MonitorList* monitors, const char* id);
MonitorInfo* find_monitor_by_device_path(const MonitorList* monitors, const char* device_path);
int get_monitor_index(const MonitorList* monitors, const MonitorInfo* monitor);

#endif // ENUM_H
