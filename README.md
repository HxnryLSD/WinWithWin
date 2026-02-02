# WinWithWin - Windows 10/11 Multi-Function Tweaker

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6?logo=windows)
![PowerShell](https://img.shields.io/badge/PowerShell-5.1%2B-5391FE?logo=powershell)

> **Tweak like a pro, whether you're a beginner or expert.**

A simple, user-friendly "all-in-one" Windows tweaker. No tweaks that lead to an unstable system. No tweaks that cause FPS drops. Open-source and fully usable offline. For performance, for privacy, for security, for everyone.

## 🎯 Features

### ✅ Safety First
- **No Instability** while tweaking windows
- **Automatic System Restore Points** before any changes
- **Registry Backup** for all modified keys
- **Undo Function** for every single tweak
- **Current State Display** - see what's enabled/disabled before changing

### 🚀 Performance
- Service optimization
- Visual effects tuning
- Power plan configuration
- Startup program management
- Scheduled task cleanup

### 🔒 Privacy
- Telemetry control
- Advertising ID management
- Activity history settings
- Location services
- App diagnostics

### 🛡️ Security
- Windows Defender configuration
- SmartScreen settings
- UAC optimization
- Update management

### 📦 Debloat
- Remove preinstalled apps (with restore option)
- Uninstall OneDrive
- Remove Cortana
- Clean up Xbox components (optional)

## 📋 Requirements

- Windows 10 (1903+) or Windows 11
- PowerShell 5.1 or later
- Administrator privileges
- .NET 8.0 Runtime (for GUI only)

## 🚀 Quick Start

### PowerShell (No GUI)
```powershell
# Run as Administrator
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope Process
.\WinWithWin.ps1
```

### GUI Application
```powershell
# Run the GUI (requires .NET 8.0 Runtime)
.\WinWithWin.GUI.exe
```

### Standalone (No .NET Required)
```powershell
# Run the standalone version - no .NET installation needed
.\WinWithWin.Standalone.exe
```

> **💡 Tipp:** Die Standalone-Version ist größer (~150MB), läuft aber auf jedem Windows-System ohne zusätzliche Abhängigkeiten. Perfekt für USB-Sticks oder Systeme ohne Internetzugang.

## 📁 Project Structure

```
WinWithWin/
├── src/
│   ├── core/              # Core PowerShell modules
│   │   ├── WinWithWin.psm1
│   │   ├── Safety.psm1
│   │   └── Helpers.psm1
│   ├── functions/         # Tweak function modules
│   │   ├── Privacy.psm1
│   │   ├── Performance.psm1
│   │   ├── Security.psm1
│   │   └── Debloat.psm1
│   └── gui/               # C# WPF Application
│       └── WinWithWin.GUI/
├── config/
│   ├── tweaks.json        # Tweak definitions
│   ├── apps.json          # Apps to debloat
│   └── presets/           # Preset configurations
├── locales/               # Localization files
│   ├── en.json
│   └── de.json
├── scripts/
│   ├── Build.ps1          # Build script
│   └── Compile.ps1        # Compile to single file
├── WinWithWin.ps1         # Main entry point
├── LICENSE
└── README.md
```

## 🎨 Presets

| Preset | Description |
|--------|-------------|
| **Balanced** | Recommended settings for most users |
| **Gaming** | Maximum performance, minimal background processes |
| **Privacy** | Maximum privacy protection |
| **Minimal** | Aggressive debloating and optimization |

## 🔧 Configuration

All tweaks are defined in JSON configuration files, making them easy to audit and customize:

```json
{
  "id": "disable_telemetry",
  "name": "Disable Telemetry",
  "category": "Privacy",
  "risk": "Safe",
  "description": "Disables Windows telemetry data collection",
  "supportedVersions": ["Windows10", "Windows11"]
}
```

## 🌍 Localization

WinWithWin supports multiple languages through JSON localization files:

- English (en) - Default
- German (de)
- More coming soon...

## ⚠️ Disclaimer

- This tool modifies Windows settings and registry values
- Always create a backup before making changes
- Some tweaks may affect Windows functionality
- Not affiliated with Microsoft

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🛠️ Building from Source

Want to compile WinWithWin yourself? Follow these instructions:

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- PowerShell 5.1 or later
- Git (optional)

### Option 1: Using the Build Script (Recommended)

The easiest way to build everything:

```powershell
# Build everything (PowerShell compiled version + GUI)
.\scripts\Build.ps1 -All

# Or build only specific components:
.\scripts\Build.ps1 -BuildGUI          # Build only the GUI (.exe)
.\scripts\Build.ps1 -CompilePS         # Compile only PowerShell modules
.\scripts\Build.ps1 -All -Configuration Debug  # Debug build
```

The compiled files will be in the `out/` directory:
- `out/gui/WinWithWin.GUI.exe` - The GUI application
- `out/WinWithWin.Compiled.ps1` - Compiled PowerShell script
- `out/config/` - Configuration files
- `out/locales/` - Localization files

### Option 2: Manual Build with dotnet CLI

```powershell
# Navigate to the GUI project
cd src\gui\WinWithWin.GUI

# Restore dependencies
dotnet restore

# Build (Debug)
dotnet build

# Build (Release)
dotnet build -c Release

# Publish as single executable
dotnet publish -c Release -o ..\..\..\out\gui
```

### Option 3: Quick PowerShell Compilation

To compile only the PowerShell modules into a single file:

```powershell
.\scripts\Compile.ps1
```

This creates `out/WinWithWin.ps1` - a standalone PowerShell script.

### Self-Contained Build (No .NET Runtime Required)

To create an .exe that doesn't require .NET to be installed:

```powershell
cd src\gui\WinWithWin.GUI
dotnet publish -c Release -r win-x64 --self-contained true -o ..\..\..\out\standalone
```

> **Note:** Self-contained builds are larger (~200MB) but run on any Windows system without .NET.

## 🤝 Contributing

Contributions are welcome! Please read our contributing guidelines before submitting a pull request.

## 🙏 Acknowledgments

Inspired by:
- [WinUtil](https://github.com/ChrisTitusTech/winutil)
- [Sophia Script](https://github.com/farag2/Sophia-Script-for-Windows)
- [Win11Debloat](https://github.com/Raphire/Win11Debloat)
