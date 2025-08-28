# MOS-DEF Usage Examples

This document provides practical examples of using MOS-DEF for common display rotation scenarios.

## Basic Operations

### List All Displays
```bash
mos-def --list
```
Output:
```
ID  NAME             CONN         RESOLUTION    ROT  KEY
M1  DELL U2720Q      DISPLAYPORT  3840x2160       0  7a1c-2f9e
M2  LG ULTRAFINE     HDMI         3840x2160      90  4f2a-9c7e
M3  ASUS PA279CV     DISPLAYPORT  3840x2160       0  c3b1-884d
```

### Rotate All Displays
```bash
# Set all displays to landscape
mos-def landscape

# Set all displays to portrait
mos-def portrait

# Toggle all displays between landscape and portrait
mos-def toggle
```

## Per-Monitor Operations

### Target Specific Monitor by Index
```bash
# Rotate only the second monitor to portrait
mos-def portrait --only M2

# Toggle first and third monitors
mos-def toggle --only M1,M3
```

### Target by Display Name
```bash
# Exact name match (use quotes for spaces)
mos-def landscape --only name:"DELL U2720Q"

# Partial name match
mos-def portrait --only name:DELL

# Multiple partial matches
mos-def toggle --only name:DELL,name:LG
```

### Target by Connection Type
```bash
# Rotate all HDMI displays
mos-def portrait --only conn:HDMI

# Rotate all DisplayPort displays
mos-def landscape --only conn:DISPLAYPORT

# Toggle internal laptop display
mos-def toggle --only conn:INTERNAL
```

### Target by Device Path Hash
```bash
# Use stable device path identifier
mos-def portrait --only path:7a1c-2f9e

# Useful for identical monitor models
mos-def landscape --only path:4f2a-9c7e,path:c3b1-884d
```

## Advanced Selection

### Include/Exclude Operations
```bash
# Rotate all DisplayPort monitors except M3
mos-def portrait --include conn:DISPLAYPORT --exclude M3

# Rotate M1 and M2, excluding any HDMI displays
mos-def landscape --include M1,M2 --exclude conn:HDMI
```

### Regular Expression Matching
```bash
# Match all DELL monitors with regex
mos-def portrait --only name:/DELL.*/

# Match monitors ending with specific model
mos-def toggle --only name:/.*U2720Q$/

# Match multiple patterns
mos-def landscape --only name:/DELL.*/,name:/LG.*/
```

## Default Monitor Settings

### Save Default Target
```bash
# Save M2 as default and set to portrait
mos-def portrait --save-default M2

# Save by name and rotate
mos-def toggle --save-default name:"DELL U2720Q"

# Save by connection type
mos-def landscape --save-default conn:INTERNAL
```

### Use Default Monitor
```bash
# After setting a default, commands operate on default only
mos-def portrait  # Only affects saved default monitor
mos-def toggle    # Only toggles saved default monitor
```

### Clear Default
```bash
# Remove saved default
mos-def --clear-default
```

## Dry Run and Verbose Output

### Preview Changes
```bash
# See what would happen without applying changes
mos-def toggle --dry-run
mos-def portrait --only M2 --dry-run

# Combine with verbose for detailed output
mos-def landscape --include M1,M3 --dry-run --verbose
```

### Detailed Output
```bash
# Get detailed information about operations
mos-def toggle --verbose
mos-def --list --verbose
```

## Real-World Scenarios

### Developer Setup
```bash
# Set main coding monitor to portrait, keep secondary in landscape
mos-def portrait --only name:"DELL U2720Q"
mos-def landscape --only name:"LG ULTRAFINE"

# Save primary monitor as default for quick toggles
mos-def --save-default name:"DELL U2720Q"
```

### Meeting/Presentation Mode
```bash
# Quickly set all external monitors to landscape for presentations
mos-def landscape --exclude conn:INTERNAL

# Return to development setup after meeting
mos-def toggle  # Assuming default was set to main monitor
```

### Troubleshooting Identical Monitors
```bash
# First identify monitors by their stable path keys
mos-def --list

# Target specific monitor when names are identical
mos-def portrait --only path:7a1c-2f9e  # Left monitor
mos-def landscape --only path:4f2a-9c7e # Right monitor
```

### Gaming Setup
```bash
# Set main gaming monitor to landscape, keep secondary portrait for chat
mos-def landscape --only M1
mos-def portrait --only M2

# Save gaming monitor as default for quick access
mos-def --save-default M1
```

## Error Handling

### When Selectors Don't Match
If no monitors match your selector, MOS-DEF will show available options:
```bash
mos-def portrait --only M5
# Error: No displays match the specified selectors.
# 
# Available displays and suggested selectors:
#   M1: DELL U2720Q
#     Use: M1
#     Or:  name:"DELL U2720Q"
#     Or:  conn:DISPLAYPORT
#     Or:  path:7a1c-2f9e
```

### Ambiguous Selections
When using `--only` with selectors that match multiple displays:
```bash
mos-def portrait --only name:DELL
# Error: --only selector matches 2 displays. Use more specific selectors:
#   M1: DELL U2720Q (use path:7a1c-2f9e for exact match)
#   M3: DELL U3421W (use path:c3b1-884d for exact match)
```

## Best Practices

1. **Use `--list` first** to understand your display setup
2. **Save defaults** for monitors you rotate frequently
3. **Use path selectors** for identical monitor models
4. **Test with `--dry-run`** before applying bulk changes
5. **Use `--verbose`** when troubleshooting issues
6. **Prefer specific selectors** (`path:` or `M#`) over partial name matches for scripts

## Integration Examples

### PowerShell Profile
```powershell
# Add to your PowerShell profile for quick access
function Set-MonitorPortrait { mos-def portrait --only M2 }
function Set-MonitorLandscape { mos-def landscape --only M2 }
function Toggle-Monitor { mos-def toggle }

Set-Alias -Name portrait -Value Set-MonitorPortrait
Set-Alias -Name landscape -Value Set-MonitorLandscape
Set-Alias -Name flip -Value Toggle-Monitor
```

### Batch Scripts
```batch
@echo off
REM Quick orientation batch files
REM dev-mode.bat
mos-def portrait --only name:"DELL U2720Q"
mos-def landscape --exclude name:"DELL U2720Q"

REM present-mode.bat
mos-def landscape
```

### Task Scheduler
Create scheduled tasks that rotate displays based on time of day or other triggers using MOS-DEF commands.
