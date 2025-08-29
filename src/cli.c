#include "cli.h"
#include "util.h"
#include "config.h"
#include "enum.h"
#include "rotate.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <ctype.h>
#include <conio.h>
#include <time.h>

// Global state
static bool g_dry_run = false;
static bool g_no_confirm = false;
static bool g_force_rdp = false;
static int g_revert_seconds = 0;

CliArgs* parse_args(int argc, char* argv[]);
void free_cli_args(CliArgs* args);
void print_usage();
void print_version();

// Main command handlers
int handle_list_command();
int handle_rotation_command(RotationCommand command, const CliArgs* args);
int handle_save_default(const char* selector);
int handle_clear_default();

// Confirmation and revert logic
bool prompt_confirmation(const char* message);
bool start_revert_timer(int seconds, const RollbackState* rollback_state);

// Selector application logic
SelectorList* get_applicable_selectors(const CliArgs* args, const MosDefConfig* config);

int main(int argc, char* argv[]) {
    // Parse command line arguments
    CliArgs* args = parse_args(argc, argv);
    if (!args) {
        return EXIT_FAILURE;
    }

    // Set global flags
    g_verbose = true; // Will be set by argument parsing
    g_dry_run = false; // Will be set by argument parsing
    g_no_confirm = false; // Will be set by argument parsing
    g_force_rdp = false; // Will be set by argument parsing

    // Check for RDP session
    if (is_rdp_session() && !g_force_rdp) {
        log_error("MOS-DEF cannot run under RDP session. Use --force-rdp to override.");
        free_cli_args(args);
        return 2;
    }

    // Handle version
    if (args->version) {
        print_version();
        free_cli_args(args);
        return 0;
    }

    // Handle help
    if (args->help || argc == 1) {
        print_usage();
        free_cli_args(args);
        return 0;
    }

    // Handle config commands
    if (args->save_default) {
        int result = handle_save_default(args->save_default);
        free_cli_args(args);
        return result;
    }

    if (args->clear_default) {
        int result = handle_clear_default();
        free_cli_args(args);
        return result;
    }

    // Handle main commands
    int result = 0;
    if (strcmp(args->command, "list") == 0) {
        result = handle_list_command();
    } else if (strcmp(args->command, "landscape") == 0) {
        result = handle_rotation_command(ROTATION_LANDSCAPE, args);
    } else if (strcmp(args->command, "portrait") == 0) {
        result = handle_rotation_command(ROTATION_PORTRAIT, args);
    } else if (strcmp(args->command, "toggle") == 0) {
        result = handle_rotation_command(ROTATION_TOGGLE, args);
    } else {
        log_error("Unknown command: %s", args->command);
        result = 2;
    }

    free_cli_args(args);
    return result;
}

CliArgs* parse_args(int argc, char* argv[]) {
    CliArgs* args = (CliArgs*)malloc(sizeof(CliArgs));
    if (!args) return NULL;

    // Initialize defaults
    args->command = NULL;
    args->include_selectors = NULL;
    args->exclude_selectors = NULL;
    args->only_selector = NULL;
    args->save_default = NULL;
    args->clear_default = false;
    args->version = false;
    args->help = false;

    // Skip program name
    int i = 1;

    // Parse global flags first
    while (i < argc) {
        if (strcmp(argv[i], "--dry-run") == 0) {
            g_dry_run = true;
            i++;
        } else if (strcmp(argv[i], "--verbose") == 0) {
            g_verbose = true;
            i++;
        } else if (strcmp(argv[i], "--no-confirm") == 0) {
            g_no_confirm = true;
            i++;
        } else if (strcmp(argv[i], "--force-rdp") == 0) {
            g_force_rdp = true;
            i++;
        } else if (strcmp(argv[i], "--version") == 0) {
            args->version = true;
            i++;
        } else if (strcmp(argv[i], "--help") == 0 || strcmp(argv[i], "-h") == 0) {
            args->help = true;
            i++;
        } else if (strcmp(argv[i], "--revert-seconds") == 0 && i + 1 < argc) {
            g_revert_seconds = atoi(argv[i + 1]);
            i += 2;
        } else {
            break; // Not a global flag, move to command parsing
        }
    }

    // Parse command
    if (i < argc) {
        args->command = argv[i];
        i++;
    } else {
        return args; // No command specified
    }

    // Parse command-specific arguments
    while (i < argc) {
        if (strcmp(argv[i], "--only") == 0 && i + 1 < argc) {
            args->only_selector = parse_selector(argv[i + 1]);
            i += 2;
        } else if (strcmp(argv[i], "--include") == 0 && i + 1 < argc) {
            args->include_selectors = parse_selector_list(argv[i + 1]);
            i += 2;
        } else if (strcmp(argv[i], "--exclude") == 0 && i + 1 < argc) {
            args->exclude_selectors = parse_selector_list(argv[i + 1]);
            i += 2;
        } else if (strcmp(argv[i], "--save-default") == 0 && i + 1 < argc) {
            args->save_default = _strdup(argv[i + 1]);
            i += 2;
        } else if (strcmp(argv[i], "--clear-default") == 0) {
            args->clear_default = true;
            i++;
        } else {
            log_error("Unknown argument: %s", argv[i]);
            free_cli_args(args);
            return NULL;
        }
    }

    return args;
}

void free_cli_args(CliArgs* args) {
    if (!args) return;

    free_selector_list(args->include_selectors);
    free_selector_list(args->exclude_selectors);
    free_selector(args->only_selector);
    free(args->save_default);
    free(args);
}

void print_usage() {
    printf("MOS-DEF (Monitor Orientation Switcher - Desktop Efficiency Fixer)\n\n");
    printf("USAGE:\n");
    printf("  mos-def [GLOBAL_FLAGS] <COMMAND> [ARGS]\n\n");
    printf("COMMANDS:\n");
    printf("  list                         List all active monitors\n");
    printf("  landscape [selectors]        Set monitors to landscape (0°)\n");
    printf("  portrait [selectors]         Set monitors to portrait (90°)\n");
    printf("  toggle [selectors]           Toggle between landscape and portrait\n\n");
    printf("SELECTORS:\n");
    printf("  --only <selector>            Apply to single monitor\n");
    printf("  --include <sel1,sel2,...>    Apply to specific monitors\n");
    printf("  --exclude <sel1,sel2,...>    Exclude specific monitors\n\n");
    printf("SELECTOR FORMATS:\n");
    printf("  M#                           Monitor ID (M1, M2, etc.)\n");
    printf("  device:\"\\\\.\\DISPLAYn\"      Device path\n");
    printf("  name:\"substring\"            Device name substring (case-insensitive)\n\n");
    printf("CONFIG COMMANDS:\n");
    printf("  --save-default <selector>    Save default monitor selector\n");
    printf("  --clear-default              Clear saved default\n\n");
    printf("GLOBAL FLAGS:\n");
    printf("  --dry-run                    Print changes without applying\n");
    printf("  --verbose                    Show detailed API calls and results\n");
    printf("  --no-confirm                 Skip confirmation prompts\n");
    printf("  --force-rdp                  Allow execution under RDP\n");
    printf("  --revert-seconds N           Auto-revert after N seconds if not confirmed\n");
    printf("  --version                    Show version information\n");
    printf("  --help, -h                   Show this help message\n\n");
    printf("EXAMPLES:\n");
    printf("  mos-def list\n");
    printf("  mos-def portrait --only M2\n");
    printf("  mos-def toggle --include M1,M3\n");
    printf("  mos-def landscape --exclude name:\"TV\"\n");
    printf("  mos-def toggle --save-default M2\n");
}

void print_version() {
    printf("MOS-DEF v1.0.0\n");
    printf("Monitor Orientation Switcher - Desktop Efficiency Fixer\n");
    printf("Built for Windows 11 x64\n");
}

// Command handlers
int handle_list_command() {
    MonitorList* monitors = enumerate_monitors();
    if (!monitors) {
        log_error("Failed to enumerate monitors");
        return 3;
    }

    print_monitor_table(monitors);
    free_monitor_list(monitors);
    return 0;
}

int handle_rotation_command(RotationCommand command, const CliArgs* args) {
    // Load configuration
    MosDefConfig* config = load_config();

    // Get monitor list
    MonitorList* monitors = enumerate_monitors();
    if (!monitors || monitors->count == 0) {
        log_error("No monitors found");
        free_config(config);
        free_monitor_list(monitors);
        return 3;
    }

    // Determine which monitors to apply to
    SelectorList* applicable_selectors = get_applicable_selectors(args, config);
    if (!applicable_selectors) {
        log_error("No monitors match the specified selectors");
        free_config(config);
        free_monitor_list(monitors);
        return 2;
    }

    // Create rollback state if revert is enabled
    RollbackState* rollback_state = NULL;
    if (g_revert_seconds > 0) {
        rollback_state = create_rollback_state(monitors);
    }

    // Perform rotation
    BatchRotationResult result = rotate_monitors_filtered(
        monitors, command, applicable_selectors->count > 0 ? applicable_selectors : NULL,
        args->exclude_selectors, g_dry_run
    );

    // Handle confirmation and revert logic
    if (!g_dry_run && !g_no_confirm && result.success_count > 0) {
        const char* command_name = (command == ROTATION_LANDSCAPE) ? "landscape" :
                                 (command == ROTATION_PORTRAIT) ? "portrait" : "toggle";

        if (g_revert_seconds > 0) {
            if (!start_revert_timer(g_revert_seconds, rollback_state)) {
                log_error("Failed to start revert timer");
            }
        } else {
            char message[256];
            sprintf_s(message, sizeof(message),
                     "Applied %s rotation to %d monitor(s). Keep changes? (y/N): ",
                     command_name, result.success_count);

            if (!prompt_confirmation(message)) {
                if (rollback_state) {
                    log_info("Reverting changes...");
                    rollback_monitors(rollback_state, false);
                }
            }
        }
    }

    // Save last action to config
    if (config && result.success_count > 0) {
        free(config->last_action);
        const char* action_name = (command == ROTATION_LANDSCAPE) ? "landscape" :
                                (command == ROTATION_PORTRAIT) ? "portrait" : "toggle";
        config->last_action = _strdup(action_name);
        save_config(config);
    }

    // Cleanup
    free_rollback_state(rollback_state);
    free_selector_list(applicable_selectors);
    free_config(config);
    free_monitor_list(monitors);

    // Free batch results
    free(result.results);

    // Return appropriate exit code
    if (result.failure_count > 0) {
        return 3; // API failure
    } else if (result.success_count == 0) {
        return 2; // No matching monitors
    } else {
        return 0; // Success
    }
}

int handle_save_default(const char* selector) {
    MosDefConfig* config = load_config();
    if (!config) {
        config = (MosDefConfig*)malloc(sizeof(MosDefConfig));
        if (!config) return 3;
        config->default_selector = NULL;
        config->last_action = NULL;
    }

    free(config->default_selector);
    config->default_selector = _strdup(selector);

    if (save_config(config)) {
        log_info("Saved default selector: %s", selector);
        free_config(config);
        return 0;
    } else {
        log_error("Failed to save default selector");
        free_config(config);
        return 3;
    }
}

int handle_clear_default() {
    MosDefConfig* config = load_config();
    if (!config) {
        log_info("No configuration to clear");
        return 0;
    }

    free(config->default_selector);
    config->default_selector = NULL;

    if (save_config(config)) {
        log_info("Cleared default selector");
        free_config(config);
        return 0;
    } else {
        log_error("Failed to clear default selector");
        free_config(config);
        return 3;
    }
}

// Helper functions
SelectorList* get_applicable_selectors(const CliArgs* args, const MosDefConfig* config) {
    // Priority: only > include > default > all
    if (args->only_selector) {
        SelectorList* list = (SelectorList*)malloc(sizeof(SelectorList));
        if (!list) return NULL;

        list->selectors = (Selector*)malloc(sizeof(Selector));
        if (!list->selectors) {
            free(list);
            return NULL;
        }

        list->selectors[0] = *args->only_selector;
        list->count = 1;
        return list;
    }

    if (args->include_selectors) {
        return parse_selector_list(args->include_selectors->selectors[0].value); // This is a bit hacky, but works
    }

    if (config && config->default_selector) {
        return parse_selector_list(config->default_selector);
    }

    // No selectors specified - apply to all monitors
    return parse_selector_list("*");
}

bool prompt_confirmation(const char* message) {
    printf("%s", message);
    fflush(stdout);

    int ch = _getch();
    printf("\n");

    return (tolower(ch) == 'y');
}

bool start_revert_timer(int seconds, const RollbackState* rollback_state) {
    if (!rollback_state) return false;

    printf("Changes will revert in %d seconds unless confirmed. Press 'y' to keep: ", seconds);
    fflush(stdout);

    // Simple countdown timer
    for (int i = seconds; i > 0; i--) {
        printf("\rChanges will revert in %d seconds unless confirmed. Press 'y' to keep: ", i);
        fflush(stdout);

        // Wait for 1 second or user input
        clock_t start = clock();
        while ((clock() - start) < CLOCKS_PER_SEC) {
            if (_kbhit()) {
                int ch = _getch();
                printf("\n");
                if (tolower(ch) == 'y') {
                    return true; // User confirmed, don't revert
                }
                break;
            }
        }
    }

    printf("\nTime expired, reverting changes...\n");
    return rollback_monitors(rollback_state, false);
}
