# MOS-DEF (Monitor Orientation Switcher – Desktop Efficiency Fixer)

A Windows 11 x64 console application for listing active monitors and setting their orientation to landscape, portrait, or toggle between them. This was built in under 3 hours from concept to executable. All critical features are fully functional, and I will be QA-ing the entire feature set, incuding some already identified trivial bugs in the coming days and weeks. Insofar as I'm aware, no other self-contained CLI utility exists that does what MOS-DEF does.

Cheers,
Krishna

## Features

- **Monitor Enumeration**: List all active monitors with their IDs, names, device paths, resolutions, and current orientations
- **Orientation Control**: Set monitors to landscape (0°), portrait (90°), or toggle between them
- **Flexible Selection**: Target specific monitors using various selector formats:
  - Monitor IDs (`M1`, `M2`, etc.)
  - Device paths (`device:"\\.\\DISPLAY1"`)
  - Device name substrings (`name:"DELL"`)
- **Configuration Management**: Save and load default monitor selections
- **Safety Features**:
  - Dry-run mode to preview changes
  - Confirmation prompts with auto-revert capability
  - RDP session detection and blocking (with override)
- **Batch Operations**: Apply changes to multiple monitors with include/exclude filters

## Building

### Prerequisites

- Windows 11 x64
- **One of the following development environments:**
  - Visual Studio 2019/2022 with C++ workload
  - Visual Studio Build Tools with C++ components
  - CMake 3.20+ with Visual Studio generator

### Quick Build (Recommended)

Simply run the provided build script:

```cmd
build.bat
```

This will automatically handle the CMake configuration and building process.

### Manual Build Steps

#### Option 1: Using Visual Studio Developer Command Prompt

```cmd
# Open Visual Studio Developer Command Prompt
# Navigate to project directory
cd C:\path\to\MOS-DEF

# Create build directory
mkdir build
cd build

# Configure with CMake
cmake .. -DCMAKE_BUILD_TYPE=Release

# Build the project
cmake --build . --config Release
```

#### Option 2: Using Visual Studio IDE

1. Open `CMakeLists.txt` in Visual Studio
2. Select Release configuration
3. Build the project

The executable will be created at `artifacts/mos-def.exe`.

### Build Requirements Notes

If you encounter compilation errors:

1. **Missing CMake**: Install CMake from https://cmake.org/download/
2. **Missing Visual Studio Tools**: Install Visual Studio Build Tools or Visual Studio Community Edition
3. **Architecture Mismatch**: Ensure you're building for x64 architecture
4. **Windows SDK**: Make sure Windows 11 SDK is installed with your Visual Studio tools

## Usage

### Basic Commands

```bash
# List all monitors
mos-def list

# Set specific monitor to portrait
mos-def portrait --only M2

# Set multiple monitors to landscape
mos-def landscape --include M1,M3

# Toggle orientation of all monitors
mos-def toggle

# Toggle with exclusions
mos-def toggle --exclude name:"TV"
```

### Configuration

```bash
# Save default monitor selection
mos-def toggle --save-default M2

# Clear saved default
mos-def --clear-default
```

### Safety Options

```bash
# Preview changes without applying
mos-def portrait --only M2 --dry-run

# Auto-revert after 30 seconds if not confirmed
mos-def toggle --revert-seconds 30

# Skip confirmation prompts
mos-def landscape --no-confirm

# Force execution under RDP
mos-def list --force-rdp
```

### Selectors

- `M#` - Monitor ID (M1, M2, etc.)
- `device:"\\.\\DISPLAYn"` - Device path
- `name:"substring"` - Device name substring (case-insensitive)

## Configuration File

Settings are stored in `%APPDATA%\MOS-DEF\config.json`:

```json
{
  "default_selector": "M2",
  "last_action": "portrait"
}
```

## Exit Codes

- `0` - Success
- `2` - Bad arguments or no matching monitors
- `3` - API failure

## API Usage

The application uses the following Win32 APIs:

- `EnumDisplayDevicesW` / `EnumDisplayDevicesA` - Enumerate display devices
- `EnumDisplaySettingsExW` / `EnumDisplaySettingsExA` - Get display settings
- `ChangeDisplaySettingsExW` / `ChangeDisplaySettingsExA` - Apply display changes

## Architecture

- **cli.c/cli.h** - Main entry point, argument parsing, user interaction
- **enum.c/enum.h** - Monitor enumeration and display formatting
- **rotate.c/rotate.h** - Display rotation logic and rollback functionality
- **config.c/config.h** - JSON configuration file handling
- **util.c/util.h** - String utilities, selector parsing, RDP detection

## License

This project is provided as-is for educational and practical use.
