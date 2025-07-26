# TinyFileSentry

<div align="center">
  <img src="TinyFileSentry.App/Resources/Icons/app-icon-128.png" alt="TinyFileSentry Logo" width="128">
  
  **A lightweight, cross-platform file monitoring and backup solution**
  
  [![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
  [![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux-blue)](#installation)
  [![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
  [![Tests](https://img.shields.io/badge/Tests-96%20passing-brightgreen)](#testing)
  [![Coverage](https://img.shields.io/badge/Coverage-96%25-success)](#testing)
</div>

## Overview

TinyFileSentry is a modern, efficient file monitoring application that automatically detects changes in your important files and creates backups with configurable post-processing actions. Built with .NET 9 and WPF, it provides a clean, intuitive interface for managing file watch rules.

### Key Features

- 🔍 **Real-time File Monitoring** - SHA-256 hash-based change detection with three configurable polling speeds (10s, 1min, 10min)
- 📁 **Flexible Backup Rules** - Set up custom source-to-destination mappings with enable/disable controls
- 🔄 **Post-Action Integration** - Automatic Git commits after successful backups
- 🎛️ **Modern WPF Interface** - Clean, responsive UI with system tray integration
- 📊 **Activity Logging** - Real-time activity log with filtering by log levels
- ⚙️ **Auto-start Support** - Optional automatic monitoring on application startup

## Installation

### Windows (Recommended)

1. **Download the latest release** from the [Releases](../../releases) page
2. **Extract** the archive to your preferred location
3. **Run** `TinyFileSentry.App.exe`

### Building from Source

#### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- Windows OS (for WPF UI) or Linux (for Core library development)

#### Build Steps
```bash
# Clone the repository
git clone https://github.com/yourusername/TinyFileSentry.git
cd TinyFileSentry

# Restore dependencies
dotnet restore

# Build the solution
dotnet build --configuration Release

# Run the application (Windows only)
dotnet run --project TinyFileSentry.App

# Or publish a self-contained executable
dotnet publish TinyFileSentry.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Quick Start

### 1. Creating Your First Watch Rule

1. **Launch TinyFileSentry** and navigate to the **Files** tab
2. **Click "Add New Rule"** to open the rule creation dialog
3. **Select Source File** - Choose the file you want to monitor
4. **Select Destination** - Choose where backups should be stored
5. **Configure Post-Action** (Optional):
   - **None** - Simple file copying
   - **Git Commit** - Automatic git commit after backup (requires git in PATH)
6. **Save** the rule

### 2. Starting Monitoring

1. **Click "Start Monitoring"** in the main window header
2. **Monitor Activity** in the Activity Log tab
3. **System Tray** - The app minimizes to system tray for background operation

### 3. Customizing Settings

Navigate to the **Settings** tab to configure:
- **Polling Speed**: Fast (10s), Medium (1min), or Slow (10min)
- **Auto-start Monitoring**: Automatically begin monitoring on app launch

## Configuration

TinyFileSentry stores its configuration in JSON format at:
- **Windows**: `%APPDATA%\TinyFileSentry\config.json`
- **Linux**: `~/.config/TinyFileSentry/config.json`

### Example Configuration
```json
{
  "pollingSpeed": "Fast",
  "isMonitoringActive": true,
  "watchRules": [
    {
      "sourceFile": "C:\\Important\\document.docx",
      "destinationRoot": "D:\\Backups\\Documents",
      "postAction": "GitCommit",
      "isEnabled": true,
      "lastCopied": "2025-06-29T10:30:00Z"
    }
  ]
}
```

## Architecture

TinyFileSentry follows **Clean Architecture** principles with clear separation of concerns:

### Project Structure
- **TinyFileSentry.Core** - Business logic, services, and domain models
- **TinyFileSentry.App** - WPF MVVM presentation layer
- **TinyFileSentry.Tests** - Comprehensive unit and integration tests

### Core Components
- **PollerService** - Orchestrates file monitoring and change detection
- **CopyService** - Handles file copying with retry logic
- **PostCopyService** - Executes post-backup actions (Git commits, etc.)
- **ConfigurationService** - Manages persistent application settings
- **LogService** - Ring buffer logging system (10,000 entries max)

### Key Design Patterns
- **Strategy Pattern** - Post-action implementations
- **Adapter Pattern** - OS abstraction layers
- **Dependency Injection** - Service composition
- **MVVM** - Clean UI/business logic separation

## File Change Detection

TinyFileSentry uses **SHA-256 hash comparison** for reliable change detection:

1. **Direct Comparison** - Computes hash of source file vs. destination file
2. **No Hash Storage** - No persistent hash storage to avoid inconsistencies
3. **Missing File Detection** - Automatically detects when destination files are missing
4. **Size Limits** - Files >10MB are automatically skipped with warnings

## Post-Actions

### Git Commit
Automatically commits backed-up files to a git repository:

**Requirements:**
- Git must be installed and available in PATH
- Destination directory must be a git repository
- Working directory should be clean (no uncommitted changes)

**Behavior:**
```bash
git add <destination_file>
git commit -m "Auto-backup from <source_file_path>"
```

## Testing

TinyFileSentry includes a comprehensive test suite with high code coverage:

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "ClassName=HashServiceTests"

# Generate coverage report
dotnet test --collect:"XPlat Code Coverage"
```

**Test Statistics:**
- **96 tests** across unit and integration suites
- **96% code coverage** of core business logic
- **Cross-platform testing** on Windows and Linux
- **Integration tests** with real file system operations

## Troubleshooting

### Common Issues

#### Monitoring Won't Start
- **Check Configuration** - Ensure `isMonitoringActive: true` in config.json
- **Verify File Paths** - Confirm source files exist and destinations are accessible
- **Check Permissions** - Ensure read access to source files and write access to destinations

#### Git Commit Failures
- **Git in PATH** - Verify git is installed and accessible from command line
- **Repository Status** - Ensure destination is a git repository (`git init` if needed)
- **Working Directory** - Check for uncommitted changes that might block commits

#### Performance Issues
- **Polling Speed** - Consider increasing polling interval for large numbers of files
- **File Size** - Files >10MB are automatically skipped
- **Antivirus** - Some antivirus software may slow file operations

### Log Analysis

Activity logs provide detailed information about:
- File change detection events
- Copy operation results
- Post-action execution status  
- Configuration changes
- Error conditions with stack traces

Access logs through the **Activity Log** tab with filtering options by log level.

## Contributing

We welcome contributions! Please follow these guidelines:

### Development Setup
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Follow the existing code style and patterns
4. Add tests for new functionality
5. Ensure all tests pass (`dotnet test`)
6. Update documentation as needed

### Code Standards
- **Clean Architecture** - Maintain separation between UI and business logic
- **Test Coverage** - Aim for >90% coverage on new code
- **Documentation** - Use XML documentation for public APIs
- **Error Handling** - Comprehensive error handling with logging

### Pull Request Process
1. Update README.md with details of changes if applicable
2. Increase version numbers following [SemVer](https://semver.org/)
3. Ensure CI pipeline passes
4. Request review from maintainers

## Roadmap

### Planned Features
- 📱 **Cross-platform UI** - Avalonia-based interface for Linux/macOS
- 🌐 **Network Destinations** - FTP, SFTP, and cloud storage support
- 🔐 **Encryption** - Optional file encryption for sensitive backups
- 📧 **Notifications** - Email/Slack notifications for backup events
- 🎯 **File Patterns** - Wildcard and regex-based file matching
- 📈 **Dashboard** - Statistics and backup history visualization
- 🔌 **Plugin System** - Custom post-action plugin support

### Version History
See [CHANGELOG.md](CHANGELOG.md) for detailed version history.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- 📝 **Issues** - Report bugs or request features via [GitHub Issues](../../issues)
- 💬 **Discussions** - Ask questions in [GitHub Discussions](../../discussions)
- 📧 **Contact** - Reach out to maintainers for security issues

## Acknowledgments

- Built with [.NET 9](https://dotnet.microsoft.com/) and [WPF](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
- UI framework powered by [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
- System tray integration via [Hardcodet.NotifyIcon.Wpf](https://github.com/hardcodet/wpf-notifyicon)
- Testing framework: [NUnit](https://nunit.org/) with [Moq](https://github.com/moq/moq4)

---

<div align="center">
  <strong>Made with ❤️ for reliable file backup automation</strong>
</div>