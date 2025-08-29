#include "util.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdarg.h>
#include <ctype.h>

bool g_verbose = false;

// RDP detection
bool is_rdp_session() {
    const char* session_name = getenv("SESSIONNAME");
    return session_name && str_starts_with(session_name, "RDP-");
}

// String utilities
bool str_starts_with(const char* str, const char* prefix) {
    if (!str || !prefix) return false;
    return strncmp(str, prefix, strlen(prefix)) == 0;
}

bool str_ends_with(const char* str, const char* suffix) {
    if (!str || !suffix) return false;
    size_t str_len = strlen(str);
    size_t suffix_len = strlen(suffix);
    if (suffix_len > str_len) return false;
    return strcmp(str + str_len - suffix_len, suffix) == 0;
}

bool str_contains(const char* str, const char* substring) {
    if (!str || !substring) return false;
    return strstr(str, substring) != NULL;
}

char* str_trim(char* str) {
    if (!str) return NULL;

    // Trim leading whitespace
    while (*str && isspace((unsigned char)*str)) {
        str++;
    }

    // Trim trailing whitespace
    char* end = str + strlen(str) - 1;
    while (end > str && isspace((unsigned char)*end)) {
        *end-- = '\0';
    }

    return str;
}

char* str_to_lower(const char* str) {
    if (!str) return NULL;
    char* result = _strdup(str);
    if (!result) return NULL;
    for (char* p = result; *p; ++p) {
        *p = (char)tolower((unsigned char)*p);
    }
    return result;
}

void str_split(const char* str, const char* delim, char*** result, int* count) {
    if (!str || !delim || !result || !count) return;

    *count = 0;
    *result = NULL;

    // Count occurrences of delimiter
    const char* temp = str;
    while ((temp = strstr(temp, delim))) {
        (*count)++;
        temp += strlen(delim);
    }
    (*count)++; // Add one for the last part

    // Allocate array
    *result = (char**)malloc(*count * sizeof(char*));
    if (!*result) return;

    // Split the string
    char* copy = _strdup(str);
    if (!copy) {
        free(*result);
        *result = NULL;
        *count = 0;
        return;
    }

    char* token = strtok(copy, delim);
    int i = 0;
    while (token && i < *count) {
        (*result)[i] = _strdup(str_trim(token));
        if (!(*result)[i]) {
            // Free allocated memory on error
            for (int j = 0; j < i; j++) {
                free((*result)[j]);
            }
            free(*result);
            *result = NULL;
            *count = 0;
            free(copy);
            return;
        }
        i++;
        token = strtok(NULL, delim);
    }

    free(copy);
}

// Selector parsing
Selector* parse_selector(const char* selector_str) {
    if (!selector_str) return NULL;

    Selector* selector = (Selector*)malloc(sizeof(Selector));
    if (!selector) return NULL;

    selector->value = NULL;

    // Check for monitor ID (M1, M2, etc.)
    if (selector_str[0] == 'M' && isdigit((unsigned char)selector_str[1])) {
        selector->type = SELECTOR_TYPE_MONITOR_ID;
        selector->value = _strdup(selector_str);
    }
    // Check for device path (device:"\\.\\DISPLAYn")
    else if (str_starts_with(selector_str, "device:\"")) {
        selector->type = SELECTOR_TYPE_DEVICE_PATH;
        // Extract the path from device:"path"
        const char* start = selector_str + 8; // Skip device:"
        const char* end = strrchr(start, '"');
        if (end) {
            size_t len = end - start;
            selector->value = (char*)malloc(len + 1);
            if (selector->value) {
                memcpy(selector->value, start, len);
                selector->value[len] = '\0';
            }
        }
    }
    // Check for device name (name:"substring")
    else if (str_starts_with(selector_str, "name:\"")) {
        selector->type = SELECTOR_TYPE_DEVICE_NAME;
        // Extract the name from name:"name"
        const char* start = selector_str + 6; // Skip name:"
        const char* end = strrchr(start, '"');
        if (end) {
            size_t len = end - start;
            selector->value = (char*)malloc(len + 1);
            if (selector->value) {
                memcpy(selector->value, start, len);
                selector->value[len] = '\0';
            }
        }
    }
    // Default to monitor ID
    else {
        selector->type = SELECTOR_TYPE_MONITOR_ID;
        selector->value = _strdup(selector_str);
    }

    if (!selector->value) {
        free(selector);
        return NULL;
    }

    return selector;
}

SelectorList* parse_selector_list(const char* selector_list_str) {
    if (!selector_list_str) return NULL;

    SelectorList* list = (SelectorList*)malloc(sizeof(SelectorList));
    if (!list) return NULL;

    char** parts = NULL;
    int count = 0;
    str_split(selector_list_str, ",", &parts, &count);

    if (count == 0 || !parts) {
        free(list);
        return NULL;
    }

    list->selectors = (Selector*)malloc(count * sizeof(Selector));
    if (!list->selectors) {
        for (int i = 0; i < count; i++) {
            free(parts[i]);
        }
        free(parts);
        free(list);
        return NULL;
    }

    list->count = 0;
    for (int i = 0; i < count; i++) {
        Selector* sel = parse_selector(parts[i]);
        if (sel) {
            list->selectors[list->count++] = *sel;
            free(sel); // Free the temporary selector struct, but keep its contents
        }
        free(parts[i]);
    }
    free(parts);

    if (list->count == 0) {
        free(list->selectors);
        free(list);
        return NULL;
    }

    return list;
}

void free_selector(Selector* selector) {
    if (selector) {
        free(selector->value);
        free(selector);
    }
}

void free_selector_list(SelectorList* list) {
    if (list) {
        for (int i = 0; i < list->count; i++) {
            free(list->selectors[i].value);
        }
        free(list->selectors);
        free(list);
    }
}

// Monitor matching
bool matches_monitor(const Selector* selector, const char* monitor_id, const char* device_path, const char* device_name) {
    if (!selector || !selector->value) return false;

    switch (selector->type) {
        case SELECTOR_TYPE_MONITOR_ID:
            return strcmp(monitor_id, selector->value) == 0;

        case SELECTOR_TYPE_DEVICE_PATH:
            return strcmp(device_path, selector->value) == 0;

        case SELECTOR_TYPE_DEVICE_NAME: {
            char* name_lower = str_to_lower(device_name);
            char* selector_lower = str_to_lower(selector->value);
            bool result = str_contains(name_lower, selector_lower);
            free(name_lower);
            free(selector_lower);
            return result;
        }

        default:
            return false;
    }
}

// Error handling
void log_error(const char* format, ...) {
    va_list args;
    va_start(args, format);
    fprintf(stderr, "ERROR: ");
    vfprintf(stderr, format, args);
    fprintf(stderr, "\n");
    va_end(args);
}

void log_info(const char* format, ...) {
    va_list args;
    va_start(args, format);
    vprintf(format, args);
    printf("\n");
    va_end(args);
}

void log_verbose(const char* format, ...) {
    if (!g_verbose) return;
    va_list args;
    va_start(args, format);
    printf("VERBOSE: ");
    vprintf(format, args);
    printf("\n");
    va_end(args);
}
