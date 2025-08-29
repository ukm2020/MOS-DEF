#include "config.h"
#include "util.h"
#include <windows.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <shlobj.h>

// Config file operations
char* get_config_file_path() {
    char* appdata_path = NULL;
    size_t appdata_len = 0;

    // Get %APPDATA% path
    if (_dupenv_s(&appdata_path, &appdata_len, "APPDATA") != 0 || !appdata_path) {
        log_error("Failed to get APPDATA environment variable");
        return NULL;
    }

    // Construct path: %APPDATA%\MOS-DEF\config.json
    const char* config_dir = "\\MOS-DEF";
    const char* config_file = "\\config.json";

    size_t total_len = strlen(appdata_path) + strlen(config_dir) + strlen(config_file) + 1;
    char* config_path = (char*)malloc(total_len);

    if (!config_path) {
        free(appdata_path);
        return NULL;
    }

    sprintf_s(config_path, total_len, "%s%s%s", appdata_path, config_dir, config_file);
    free(appdata_path);

    // Ensure the directory exists
    char* dir_end = strrchr(config_path, '\\');
    if (dir_end) {
        *dir_end = '\0';
        CreateDirectoryA(config_path, NULL);
        *dir_end = '\\';
    }

    return config_path;
}

MosDefConfig* load_config() {
    char* config_path = get_config_file_path();
    if (!config_path) return NULL;

    FILE* file = NULL;
    if (fopen_s(&file, config_path, "r") != 0 || !file) {
        free(config_path);
        // Return empty config if file doesn't exist
        MosDefConfig* config = (MosDefConfig*)malloc(sizeof(MosDefConfig));
        if (config) {
            config->default_selector = NULL;
            config->last_action = NULL;
        }
        return config;
    }

    // Read entire file
    fseek(file, 0, SEEK_END);
    long file_size = ftell(file);
    fseek(file, 0, SEEK_SET);

    char* json_content = (char*)malloc(file_size + 1);
    if (!json_content) {
        fclose(file);
        free(config_path);
        return NULL;
    }

    size_t bytes_read = fread(json_content, 1, file_size, file);
    json_content[bytes_read] = '\0';
    fclose(file);
    free(config_path);

    MosDefConfig* config = json_to_config(json_content);
    free(json_content);

    return config;
}

bool save_config(const MosDefConfig* config) {
    if (!config) return false;

    char* config_path = get_config_file_path();
    if (!config_path) return false;

    char* json_content = config_to_json(config);
    if (!json_content) {
        free(config_path);
        return false;
    }

    FILE* file = NULL;
    if (fopen_s(&file, config_path, "w") != 0 || !file) {
        free(config_path);
        free(json_content);
        return false;
    }

    fprintf(file, "%s", json_content);
    fclose(file);

    free(config_path);
    free(json_content);
    return true;
}

void free_config(MosDefConfig* config) {
    if (config) {
        free(config->default_selector);
        free(config->last_action);
        free(config);
    }
}

// JSON parsing (simple implementation for our specific format)
char* json_escape_string(const char* str) {
    if (!str) return _strdup("null");

    // Calculate required size (worst case: every char escaped)
    size_t len = strlen(str);
    char* escaped = (char*)malloc(len * 2 + 3); // +3 for quotes and null terminator
    if (!escaped) return NULL;

    char* dest = escaped;
    *dest++ = '"';

    for (const char* src = str; *src; src++) {
        switch (*src) {
            case '"':
                *dest++ = '\\';
                *dest++ = '"';
                break;
            case '\\':
                *dest++ = '\\';
                *dest++ = '\\';
                break;
            case '\n':
                *dest++ = '\\';
                *dest++ = 'n';
                break;
            case '\r':
                *dest++ = '\\';
                *dest++ = 'r';
                break;
            case '\t':
                *dest++ = '\\';
                *dest++ = 't';
                break;
            default:
                *dest++ = *src;
                break;
        }
    }

    *dest++ = '"';
    *dest = '\0';

    return escaped;
}

char* json_unescape_string(const char* json_str) {
    if (!json_str || strcmp(json_str, "null") == 0) return NULL;

    // Remove surrounding quotes
    if (*json_str != '"' || json_str[strlen(json_str) - 1] != '"') {
        return _strdup(json_str);
    }

    const char* start = json_str + 1;
    size_t len = strlen(json_str) - 2;
    char* unescaped = (char*)malloc(len + 1);
    if (!unescaped) return NULL;

    char* dest = unescaped;
    for (size_t i = 0; i < len; i++) {
        if (start[i] == '\\' && i + 1 < len) {
            switch (start[i + 1]) {
                case '"':
                    *dest++ = '"';
                    i++;
                    break;
                case '\\':
                    *dest++ = '\\';
                    i++;
                    break;
                case 'n':
                    *dest++ = '\n';
                    i++;
                    break;
                case 'r':
                    *dest++ = '\r';
                    i++;
                    break;
                case 't':
                    *dest++ = '\t';
                    i++;
                    break;
                default:
                    *dest++ = start[i];
                    break;
            }
        } else {
            *dest++ = start[i];
        }
    }
    *dest = '\0';

    return unescaped;
}

char* config_to_json(const MosDefConfig* config) {
    if (!config) return _strdup("{}");

    char* default_selector_json = json_escape_string(config->default_selector);
    char* last_action_json = json_escape_string(config->last_action);

    size_t json_len = strlen(default_selector_json) + strlen(last_action_json) + 50;
    char* json = (char*)malloc(json_len);
    if (!json) {
        free(default_selector_json);
        free(last_action_json);
        return NULL;
    }

    sprintf_s(json, json_len, "{\n  \"default_selector\": %s,\n  \"last_action\": %s\n}",
              default_selector_json, last_action_json);

    free(default_selector_json);
    free(last_action_json);
    return json;
}

MosDefConfig* json_to_config(const char* json) {
    if (!json) return NULL;

    MosDefConfig* config = (MosDefConfig*)malloc(sizeof(MosDefConfig));
    if (!config) return NULL;

    config->default_selector = NULL;
    config->last_action = NULL;

    // Simple JSON parser for our specific format
    const char* pos = json;

    // Skip whitespace
    while (*pos && isspace((unsigned char)*pos)) pos++;

    if (*pos != '{') {
        free_config(config);
        return NULL;
    }
    pos++;

    while (*pos) {
        // Skip whitespace
        while (*pos && isspace((unsigned char)*pos)) pos++;

        if (*pos == '}') break;

        // Parse key
        if (*pos != '"') {
            free_config(config);
            return NULL;
        }
        pos++;

        const char* key_start = pos;
        while (*pos && *pos != '"') pos++;
        if (!*pos) {
            free_config(config);
            return NULL;
        }

        size_t key_len = pos - key_start;
        char* key = (char*)malloc(key_len + 1);
        if (!key) {
            free_config(config);
            return NULL;
        }
        memcpy(key, key_start, key_len);
        key[key_len] = '\0';
        pos++;

        // Skip whitespace and colon
        while (*pos && (isspace((unsigned char)*pos) || *pos == ':')) pos++;

        // Parse value
        if (*pos == '"') {
            pos++;
            const char* value_start = pos;
            bool escaped = false;
            while (*pos) {
                if (*pos == '"' && !escaped) break;
                escaped = (*pos == '\\');
                pos++;
            }
            if (!*pos) {
                free(key);
                free_config(config);
                return NULL;
            }

            size_t value_len = pos - value_start;
            char* value_json = (char*)malloc(value_len + 3); // +3 for quotes and null
            if (!value_json) {
                free(key);
                free_config(config);
                return NULL;
            }
            value_json[0] = '"';
            memcpy(value_json + 1, value_start, value_len);
            value_json[value_len + 1] = '"';
            value_json[value_len + 2] = '\0';

            char* value = json_unescape_string(value_json);
            free(value_json);

            if (strcmp(key, "default_selector") == 0) {
                config->default_selector = value;
            } else if (strcmp(key, "last_action") == 0) {
                config->last_action = value;
            } else {
                free(value);
            }

            pos++;
        } else if (strncmp(pos, "null", 4) == 0) {
            pos += 4;
        }

        free(key);

        // Skip whitespace and comma
        while (*pos && (isspace((unsigned char)*pos) || *pos == ',')) pos++;
    }

    return config;
}
