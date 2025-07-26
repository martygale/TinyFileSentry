# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Essential Commands

```bash
# Build and test
dotnet restore                              # Restore dependencies
dotnet build                               # Build all projects
dotnet build --configuration Release      # Release build
dotnet test                               # Run all tests
dotnet test --verbosity normal           # Run tests with detailed output

# Single test execution
dotnet test --filter "TestMethodName"
dotnet test --filter "ClassName"
dotnet test TinyFileSentry.Tests/Services/HashServiceTests.cs

# Application publishing
dotnet publish TinyFileSentry.App -c Release -r win-x64

# Run the WPF application (Windows only)
dotnet run --project TinyFileSentry.App
```

## Architecture Overview

TinyFileSentry follows a **Clean Architecture** pattern with clear separation between UI and business logic:

### Project Structure
- **TinyFileSentry.Core**: Business logic library containing all services, models, and interfaces
- **TinyFileSentry.App**: WPF MVVM application with UI and presentation logic  
- **TinyFileSentry.Tests**: Comprehensive test suite with Unit and Integration tests

### Core Services Architecture
The application is built around these key services, orchestrated through dependency injection:

1. **PollerService**: Central orchestrator that monitors files, detects changes via SHA-256 hashing. Uses `pollingSpeed.ToSeconds()` for interval calculation
2. **CopyService**: Handles file copying with retry logic (0.5s intervals, max 5s total)
3. **PostCopyService**: Executes post-actions using Strategy pattern (None/GitCommit)
4. **RulesService**: Manages watch rules (CRUD operations)
5. **LogService**: Ring buffer logging (10,000 entries max)
6. **ConfigurationService**: JSON persistence to %APPDATA%\TinyFileSentry\ with automatic enum validation

### Key Design Patterns
- **Strategy Pattern**: PostAction implementations (None, GitCommit)
- **Adapter Pattern**: OS abstractions (IFileSystem, IClock, IProcessRunner)
- **Dependency Injection**: Custom ServiceContainer for loose coupling
- **Extension Methods**: PollingSpeedExtensions for clean enum-to-value conversions

### New Core Components
- **Models/PollingSpeed.cs**: Enum defining three polling intervals with JsonStringEnumConverter
- **Extensions/PollingSpeedExtensions.cs**: Conversion methods between enum, seconds, and UI indices
- **Enhanced Configuration**: Added `isMonitoringActive` field for persistent auto-start behavior

## Business Rules & Constraints

- Files >10MB are automatically skipped with warning logs
- **Polling Speed**: Three configurable intervals - Slow (600s), Medium (60s), Fast (10s)
- File paths are sanitized for cross-platform compatibility
- SHA-256 hashing ensures file integrity verification
- GitCommit requires git in PATH, executes: `git add <file> && git commit -m "Auto-backup from <source_file_path>"`
- **Auto-start**: Monitoring automatically starts at application launch if `isMonitoringActive` is true

### File Change Detection Logic

**IMPORTANT**: The system uses **direct hash comparison** between source and destination files, NOT stored hashes:

1. **No Hash Storage**: LastHash property was removed from WatchRule model - no hashes are persisted
2. **Real-time Comparison**: `HasFileChanged()` computes SHA-256 of source file and destination file, compares them directly
3. **Missing Destination**: If destination file doesn't exist, change is detected (copy needed)
4. **Source Deleted**: If source file is deleted, rule status becomes `SourceDeleted`

This approach eliminates false positives from stored hash inconsistencies and provides reliable change detection.

### Polling Speed Management

**Key Implementation Details**:
- **PollingSpeedExtensions.cs**: Provides conversion methods between enum values and UI indices
- **UI Integration**: Settings tab ComboBox with three options binding to `PollingSpeedIndex` property
- **Automatic Conversion**: `pollingSpeed.ToSeconds()` extension method provides interval in seconds
- **No Duplication**: Configuration stores only `pollingSpeed` enum, not separate interval value
- **Validation**: ConfigurationService validates enum values and defaults to Fast on invalid input

## Configuration Format

### Configuration Fields

- **pollingSpeed**: Enum value
- **isMonitoringActive**: Boolean - whether monitoring should start automatically at app launch
- **autoStart**: Boolean - whether application should start with Windows
- **watchRules**: Array of file watch configurations

**Important Notes**: 
- `lastHash` field was removed from configuration format as part of the new direct hash comparison approach
- `pollingIntervalSec` was removed to avoid duplication - interval is derived from `pollingSpeed` enum

## Testing Strategy

- **Unit Tests**: Service layer with Moq for interface mocking
- **Integration Tests**: Real file system operations in isolated temp directories  
- **Test Coverage**: >80% requirement with NUnit framework
- **Code Comments**: All code comments must be written in English language
- **Log Messages**: All log messages and console outputs must be in English, regardless of the code language
- **Configuration Testing**: Comprehensive tests for PollingSpeed enum conversions and validation
- **Extension Method Testing**: TestCase attributes for verifying PollingSpeedExtensions conversion logic
- **Cross-Platform Testing**: Library is tested on both Windows and Linux
  - Path handling differences: `Path.GetDirectoryName("C:\file.txt")` returns `"C:\"` on Windows vs `""` on Linux
  - File attribute differences: Windows has ReadOnly attributes that need special handling for cleanup
  - Use `Path.GetDirectoryName()` in tests instead of hardcoding expected paths
  - Use `RemoveReadOnlyAttributes()` helper for Windows git repository cleanup
  - Mock setups must account for platform-specific behavior
- **App Project Testing**: Classes in TinyFileSentry.App project (WPF/UI related) are not unit tested due to framework compatibility constraints

## Development Notes

- Project targets .NET 9.0 with modern  WPF UI
- EnableWindowsTargeting=true allows Linux development/CI
- TreatWarningsAsErrors=true enforces code quality
- Self-contained publishing creates single-file executable

## MCP Context7 Usage

**Context7** provides access to up-to-date library documentation and code examples from official repositories. Use it when:

- Working with new APIs or unfamiliar libraries
- Debugging complex issues or looking for best practices  
- Need current examples and documentation beyond basic references
- Investigating framework-specific patterns (MVVM, ETW tracing, etc.)

Access via: `mcp__context7__resolve-library-id` → `mcp__context7__get-library-docs`

## WPF Application Architecture

### UI Structure
The WPF application implements a modern MVVM architecture with the following structure:

- **MainWindow.xaml**: Main application window with tab-based navigation
  - Files tab: CRUD operations for watch rules with DataGrid
  - Activity Log tab: Filtered log display with real-time updates
  - Settings tab: Application configuration interface

- **Views/RuleEditWindow.xaml**: Modal dialog for rule creation/editing
  - Step-by-step form with validation
  - File/folder picker integration
  - Real-time path validation

### MVVM Implementation
- **ViewModels/MainViewModel.cs**: Main application state and commands
- **ViewModels/WatchRuleViewModel.cs**: Rule representation with status binding
- **ViewModels/LogEntryViewModel.cs**: Log entry display with level coloring
- Uses CommunityToolkit.Mvvm for command binding and property notifications

### UI Styling System
Located in `Styles/` directory with modular design:
- **Colors.xaml**: Centralized color palette with system color integration
- **WindowStyles.xaml**: Window and modal styling
- **ButtonStyles.xaml**: Button variants (primary, secondary, icon, toggle)
- **TabStyles.xaml**: Modern tab control styling
- **DataGridStyles.xaml**: DataGrid with hover effects and custom headers
- **IconStyles.xaml**: SVG-based vector icons as DrawingImage resources

### System Integration
- **System Tray**: Hardcodet.NotifyIcon.Wpf for background operation
- **File Dialogs**: Windows Forms integration for file/folder selection
- **Responsive Design**: Adaptive layout with minimum window sizes
- **Accessibility**: Screen reader support and keyboard navigation

### UI Design Principles
- **Однозначность**: Clear visual hierarchy and obvious interactions
- **Мгновенная обратная связь**: Immediate visual feedback for all actions
- **Стабильная иерархия**: Left-to-right, top-to-bottom visual flow
- **Сдержанный минимализм**: System colors with single accent color
- **Доступность**: Full keyboard navigation and narrator support

### Resource Loading Order
**CRITICAL**: Colors.xaml must load first to prevent DependencyProperty.UnsetValue errors:
```xml
<ResourceDictionary.MergedDictionaries>
    <ResourceDictionary Source="Styles/Colors.xaml"/>      <!-- FIRST -->
    <ResourceDictionary Source="Styles/IconStyles.xaml"/>
    <ResourceDictionary Source="Styles/WindowStyles.xaml"/>
    <!-- Other styles... -->
</ResourceDictionary.MergedDictionaries>
```

### Common UI Issues & Solutions
1. **Resource loading errors**: Ensure Colors.xaml loads first
2. **System color fallbacks**: Colors.xaml includes hardcoded fallbacks
3. **File locking during build**: Terminate running processes before rebuild
4. **Modal dialog ownership**: Always set Owner property for proper Z-order

### Service Integration
- Manual DI setup in MainWindow.xaml.cs constructor
- Async command handling for PollerService operations
- Event-driven updates from Core services to ViewModels
- Proper Dispatcher.BeginInvoke for cross-thread UI updates

### Auto-start and State Management
- **Auto-start Logic**: PollerService automatically starts at application launch if `IsMonitoringActive` is true
- **State Persistence**: Monitoring state is saved to configuration when toggled via UI
- **Settings Synchronization**: `LoadSettings()` and `SaveSettings()` methods handle pollingSpeed and monitoring state
- **UI Binding**: MainViewModel properties are bound to configuration values and persist across app restarts

## UI Layout & Responsive Design

### Adaptive Layout Patterns
- **Settings Tab**: Uses Grid with proportional columns (2*:Auto:1*) for adaptive spacing between labels and controls
- **Activity Log**: CheckBoxes with VerticalAlignment="Center" for proper alignment with text labels
- **Modal Dialogs**: ScrollViewer wrapping main content to handle overflow while keeping headers/footers fixed

### Space Optimization Guidelines
- **Margins**: 24px for main container, 20px for card padding, 16px between cards
- **Font Sizes**: Headers 15-20px, descriptions 13px, body text 14px
- **Vertical Spacing**: 6px between related elements, 12-16px between sections
- **Content Areas**: MaxWidth 800px for optimal readability, ScrollViewer for overflow handling

## Common Issues & Troubleshooting

### Monitoring Not Starting
- **Check IsMonitoringActive**: Ensure `isMonitoringActive: true` in config.json
- **Auto-start Implementation**: MainWindow constructor loads `IsMonitoringActive` from config and starts PollerService if needed
- **Log Investigation**: Look for "Configuration loaded successfully" and "Poller service started" messages

### Configuration Issues  
- **Missing pollingSpeed**: Defaults to "Medium" if invalid or missing
- **Legacy pollingIntervalSec**: No longer used - remove from config.json if present
- **State Persistence**: Changes to monitoring state are automatically saved to config

### Build Errors
- **File Locking**: Stop running application before rebuild to avoid MSB3021 errors
- **Dependency Issues**: Run `dotnet clean && dotnet build` to resolve assembly conflicts

## Icon Management

### Icon Conversion Workflow

**Converting SVG to PNG/ICO with proper transparency:**

```bash
# Convert SVG to PNG with transparent background
cd TinyFileSentry.App/Resources/Icons
convert app-icon.svg -background none app-icon-256.png

# Generate all required sizes preserving transparency
for size in 128 64 48 32 16; do 
    convert app-icon-256.png -resize ${size}x${size} app-icon-${size}.png
done

# Create multi-resolution ICO file
convert app-icon-16.png app-icon-32.png app-icon-48.png app-icon-64.png app-icon-128.png app-icon-256.png app-icon.ico
```

