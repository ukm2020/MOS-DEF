# MOS-DEF Build Status

## Project Overview

✅ **Complete** - MOS-DEF v0.1.0 implementation finished

### Core Components Status

| Component | Status | Description |
|-----------|--------|-------------|
| 🏗️ Solution Structure | ✅ Complete | .NET 8 solution with proper project organization |
| 🔧 PInvoke Layer | ✅ Complete | Windows Display Configuration API bindings |
| 🎯 Display Discovery | ✅ Complete | Monitor enumeration and information gathering |
| 🔍 Selector System | ✅ Complete | M#, name:, conn:, path: selector parsing and matching |
| ⚙️ Configuration Manager | ✅ Complete | %AppData%\MOS-DEF\config.json persistence |
| 💻 CLI Interface | ✅ Complete | Argument parsing and command execution |
| 🧪 Unit Tests | ✅ Complete | Comprehensive test coverage for core functionality |
| 📦 Build System | ✅ Complete | PowerShell scripts and GitHub Actions CI/CD |
| 📚 Documentation | ✅ Complete | README, examples, and inline documentation |

### Features Implemented

#### v0.1.0 - Core CLI Functionality
- ✅ Basic rotation commands (landscape, portrait, toggle)
- ✅ Display listing with --list
- ✅ Per-monitor targeting with --only, --include, --exclude
- ✅ Default selector persistence with --save-default and --clear-default
- ✅ Dry run mode with --dry-run
- ✅ Verbose output with --verbose
- ✅ Comprehensive selector formats (M#, name:, conn:, path:, regex)
- ✅ Single-file executable packaging
- ✅ GitHub Actions CI/CD with artifact publishing

#### Planned for v0.2.0
- 🔄 270-degree rotation support (ROTATE270)
- 📁 Per-monitor overrides configuration file
- 🔧 Additional selector improvements

#### Planned for v0.3.0
- 🖥️ Optional WinForms GUI
- 🖱️ System tray integration
- ⌨️ Hotkey support

## Build Instructions

### Prerequisites
- .NET 8 SDK
- Windows 11 development environment
- PowerShell 5.1+ (for build scripts)

### Quick Build
```powershell
# Clone and build
git clone <repository-url>
cd MOS-DEF
.\scripts\build.ps1
```

### Advanced Build Options
```powershell
# Build with tests
.\scripts\build.ps1 -Test

# Clean build
.\scripts\build.ps1 -Clean

# Verbose output
.\scripts\build.ps1 -Verbose
```

### Testing
```powershell
# Run all tests
.\scripts\test.ps1

# Run with coverage
.\scripts\test.ps1 -Coverage

# Watch mode for development
.\scripts\test.ps1 -Watch
```

## Usage Examples

### Basic Operations
```bash
# List all displays
mos-def --list

# Set all to portrait
mos-def portrait

# Toggle specific monitor
mos-def toggle --only M2
```

### Advanced Selector Usage
```bash
# Target by name
mos-def landscape --only name:"DELL U2720Q"

# Target by connection
mos-def portrait --only conn:DISPLAYPORT

# Exclude specific monitors
mos-def toggle --include conn:DISPLAYPORT --exclude M1
```

### Default Monitor Management
```bash
# Save default
mos-def portrait --save-default M2

# Use default (operates only on M2)
mos-def toggle

# Clear default
mos-def --clear-default
```

## Technical Architecture

### Project Structure
```
MOS-DEF/
├── src/
│   ├── MosDef.Core/          # Core business logic and PInvoke
│   ├── MosDef.Cli/           # Command line interface
│   └── MosDef.Gui/           # GUI placeholder for v0.3.0
├── tests/
│   └── MosDef.Tests/         # Unit tests
├── scripts/                  # Build and test automation
└── .github/workflows/        # CI/CD automation
```

### Key Classes
- `User32DisplayConfig` - Windows API PInvoke layer
- `DisplayManager` - High-level display operations
- `SelectorParser` - Monitor selector parsing and matching
- `ConfigManager` - Configuration persistence
- `ArgumentParser` - CLI argument processing
- `CommandExecutor` - Command execution logic

## Installation

### From Release
1. Download `mos-def-vX.X.X-win-x64.exe` from GitHub Releases
2. Place in desired location (e.g., `C:\Tools\`)
3. Add to PATH for global access
4. Run `mos-def --help` to verify installation

### From Source
1. Clone repository
2. Run `.\scripts\build.ps1`
3. Find executable in `artifacts/mos-def.exe`

## Security & Compliance

- ✅ No network access required
- ✅ No telemetry or data collection
- ✅ No elevation required on typical systems
- ✅ MIT License - Open source
- ⚠️ Corporate environments may block display changes via Group Policy

## Troubleshooting

### Common Issues

1. **"No .NET SDKs were found"**
   - Install .NET 8 SDK from https://dotnet.microsoft.com/download

2. **Display changes don't apply**
   - Check Group Policy restrictions
   - Try running as Administrator
   - Verify displays support rotation

3. **Config file errors**
   - Delete `%AppData%\MOS-DEF\config.json` to reset
   - Check file permissions

### Getting Help

- Check `mos-def --help` for usage
- Review `EXAMPLES.md` for advanced scenarios
- File issues on GitHub repository

## Contributing

The core v0.1.0 functionality is complete, but contributions are welcome for:
- Bug fixes and improvements
- Documentation enhancements
- v0.2.0 and v0.3.0 features
- Additional test coverage

## License

MIT License - See LICENSE file for details.
