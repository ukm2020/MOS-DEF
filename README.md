# MOS-DEF (Monitor Orientation Switcher - Desktop Efficiency Fixer)

A Windows 11 utility that sets display rotation to Portrait or Landscape in one click or one command.

## What MOS-DEF Does

MOS-DEF provides instant display rotation control for Windows 11 systems with:
- One-command rotation: `mos-def portrait`, `mos-def landscape`, `mos-def toggle`
- Per-monitor targeting: specify exactly which monitors to rotate
- Persistent settings: changes are saved to Windows display configuration
- No elevation required on typical systems

## Safety Notes

- **Windows 11 only**: This utility is designed and tested for Windows 11 systems
- **No network access**: MOS-DEF operates entirely offline with no telemetry
- **Enterprise environments**: Group policy may block display changes in some corporate environments
- **Active displays only**: Only connected and active displays are modified

## Usage Examples

### Basic Commands
```bash
# Set all displays to landscape
mos-def landscape

# Set all displays to portrait
mos-def portrait

# Toggle between landscape and portrait
mos-def toggle

# List all available displays
mos-def --list
```

### Per-Monitor Targeting
```bash
# Rotate only the second monitor
mos-def portrait --only M2

# Rotate specific monitor by name
mos-def toggle --only name:"DELL U2720Q"

# Rotate multiple monitors
mos-def landscape --include M1,M3

# Rotate all except one monitor
mos-def portrait --exclude M2
```

### Default Monitor Settings
```bash
# Save M2 as default target
mos-def toggle --save-default M2

# Clear saved default
mos-def --clear-default

# When default is set, commands operate on default monitor only
mos-def portrait  # operates on saved default monitor
```

### Monitor Selectors

- `M#`: MOS-DEF index (M1, M2, M3) assigned left to right
- `name:"string"` or `name:partial`: Match by display name
- `conn:HDMI` or `conn:DISPLAYPORT` or `conn:INTERNAL`: Match by connection type
- `path:<hash>`: Match by device path hash for stability
- `name:/regex/`: Advanced regex matching

### Global Flags
```bash
# Verbose output
mos-def portrait --verbose

# Dry run (show what would happen)
mos-def toggle --dry-run
```

## Troubleshooting

### Display Changes Don't Apply
- Ensure Windows display settings aren't locked by group policy
- Try running from an elevated command prompt if in a corporate environment
- Check that the target monitor supports the requested rotation

### Monitor Not Found
- Use `mos-def --list` to see available monitors and their identifiers
- For monitors with identical names, use the `path:` selector with the hash from `--list`
- Ensure the monitor is connected and active in Windows display settings

### Configuration Issues
- Configuration is stored in `%AppData%\MOS-DEF\config.json`
- Delete this file to reset all settings if needed

## Build Instructions

### Prerequisites
- .NET 8 SDK
- Windows 11 development environment

### Building
```bash
# Debug build
dotnet build

# Release build with single-file executable
dotnet publish src/MosDef.Cli -c Release -r win-x64 /p:PublishSingleFile=true /p:PublishTrimmed=true
```

The output executable will be `src/MosDef.Cli/bin/Release/net8.0/win-x64/publish/mos-def.exe`

### Running Tests
```bash
dotnet test
```

## CI/CD

This project uses GitHub Actions for continuous integration:
- Builds on every push and pull request
- Creates release artifacts on tagged versions
- Uploads single-file executable as build artifact

## License

GPL-3.0 License - see [LICENSE](LICENSE) file for details.

## Version History

- **v0.1.0**: CLI with per-monitor flags, list command, defaults, and CI artifacts
- **v0.2.0** (planned): Add 270-degree rotation support and per-monitor overrides file
- **v0.3.0** (planned): Optional WinForms GUI with Portrait, Landscape, Toggle buttons
