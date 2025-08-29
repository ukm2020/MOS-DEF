#ifndef CONFIG_H
#define CONFIG_H

#include <stdbool.h>

// Configuration structure
typedef struct {
    char* default_selector;
    char* last_action;
} MosDefConfig;

// Config file operations
char* get_config_file_path();
MosDefConfig* load_config();
bool save_config(const MosDefConfig* config);
void free_config(MosDefConfig* config);

// JSON parsing (simple implementation for our specific format)
char* json_escape_string(const char* str);
char* json_unescape_string(const char* str);
char* config_to_json(const MosDefConfig* config);
MosDefConfig* json_to_config(const char* json);

#endif // CONFIG_H
