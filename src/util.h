#ifndef UTIL_H
#define UTIL_H

#include <windows.h>
#include <stdbool.h>
#include <stdint.h>

// RDP detection
bool is_rdp_session();

// String utilities
bool str_starts_with(const char* str, const char* prefix);
bool str_ends_with(const char* str, const char* suffix);
bool str_contains(const char* str, const char* substring);
char* str_trim(char* str);
char* str_to_lower(const char* str);
void str_split(const char* str, const char* delim, char*** result, int* count);

// Selector types and parsing
typedef enum {
    SELECTOR_TYPE_MONITOR_ID,
    SELECTOR_TYPE_DEVICE_PATH,
    SELECTOR_TYPE_DEVICE_NAME
} SelectorType;

typedef struct {
    SelectorType type;
    char* value;
} Selector;

typedef struct {
    Selector* selectors;
    int count;
} SelectorList;

// Selector parsing
Selector* parse_selector(const char* selector_str);
SelectorList* parse_selector_list(const char* selector_list_str);
void free_selector(Selector* selector);
void free_selector_list(SelectorList* list);

// Monitor matching
bool matches_monitor(const Selector* selector, const char* monitor_id, const char* device_path, const char* device_name);

// Error handling
void log_error(const char* format, ...);
void log_info(const char* format, ...);
void log_verbose(const char* format, ...);

// Global verbose flag
extern bool g_verbose;

#endif // UTIL_H
