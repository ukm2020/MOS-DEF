#ifndef CLI_H
#define CLI_H

#include "util.h"
#include "config.h"
#include "enum.h"
#include "rotate.h"

// CLI argument structure
typedef struct {
    const char* command;
    SelectorList* include_selectors;
    SelectorList* exclude_selectors;
    Selector* only_selector;
    char* save_default;
    bool clear_default;
    bool version;
    bool help;
} CliArgs;

// CLI functions
CliArgs* parse_args(int argc, char* argv[]);
void free_cli_args(CliArgs* args);
void print_usage();
void print_version();

#endif // CLI_H
