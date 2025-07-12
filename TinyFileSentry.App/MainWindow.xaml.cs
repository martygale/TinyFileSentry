using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Hardcodet.Wpf.TaskbarNotification;
using TinyFileSentry.App.ViewModels;
using TinyFileSentry.App.Views;
using TinyFileSentry.Core.Models;
using TinyFileSentry.Core.Services;

namespace TinyFileSentry.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly LogService _logService;
    
    public MainWindow()
    {
        InitializeComponent();
        
        // Create services (in real application would be through DI)
        var clock = new TinyFileSentry.Core.Adapters.SystemClock();
        var fileSystem = new TinyFileSentry.Core.Adapters.FileSystemAdapter();
        _logService = new LogService(clock);
        var configService = new ConfigurationService(fileSystem, _logService);
        var rulesService = new RulesService(configService, _logService);
        var pathSanitizer = new PathSanitizer(fileSystem);
        var copyService = new CopyService(fileSystem, _logService, pathSanitizer);
        var hashService = new HashService(fileSystem);
        var processRunner = new TinyFileSentry.Core.Adapters.ProcessRunner();
        var postCopyService = new PostCopyService(processRunner, _logService, fileSystem);
        
        var pollerService = new PollerService(
            rulesService, 
            fileSystem, 
            hashService, 
            copyService, 
            postCopyService, 
            _logService, 
            clock, 
            configService, 
            pathSanitizer);
        
        _viewModel = new MainViewModel(pollerService, rulesService, _logService, configService);
        DataContext = _viewModel;
        
        // Update monitoring UI state
        UpdateMonitoringStatus();
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        
        // Load settings to UI
        LoadSettingsUI();
        
        // Setup system tray icon
        SetupTrayIcon();
        
        // Hide to system tray on close
        Closing += OnWindowClosing;
        StateChanged += OnWindowStateChanged;
        
        // Check empty state
        UpdateEmptyState();
        _viewModel.WatchRules.CollectionChanged += (s, e) => UpdateEmptyState();
    }
    
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsMonitoringActive))
        {
            UpdateMonitoringStatus();
        }
    }
    
    private void UpdateMonitoringStatus()
    {
        if (_viewModel.IsMonitoringActive)
        {
            StatusText.Text = "Monitoring active";
            StatusText.Foreground = (System.Windows.Media.Brush)FindResource("SuccessBrush");
            PauseResumeButton.IsChecked = false;
            PauseResumeIcon.Source = (System.Windows.Media.ImageSource)FindResource("PauseIcon");
            PauseResumeText.Text = "Pause";
        }
        else
        {
            StatusText.Text = "Monitoring paused";
            StatusText.Foreground = (System.Windows.Media.Brush)FindResource("WarningBrush");
            PauseResumeButton.IsChecked = true;
            PauseResumeIcon.Source = (System.Windows.Media.ImageSource)FindResource("PlayIcon");
            PauseResumeText.Text = "Resume";
        }
        
        // Update tray context menu  
        // PauseResumeMenuItem.Header = _viewModel.IsMonitoringActive ? "Pause" : "Resume";
    }
    
    private void UpdateEmptyState()
    {
        if (_viewModel.WatchRules.Count == 0)
        {
            EmptyState.Visibility = Visibility.Visible;
            RulesDataGrid.Visibility = Visibility.Collapsed;
        }
        else
        {
            EmptyState.Visibility = Visibility.Collapsed;
            RulesDataGrid.Visibility = Visibility.Visible;
        }
    }
    
    // UI event handlers
    private async void PauseResumeButton_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.ToggleMonitoringCommand.ExecuteAsync(null);
    }
    
    
    private void AddRuleButton_Click(object sender, RoutedEventArgs e)
    {
        var ruleEditWindow = new RuleEditWindow();
        ruleEditWindow.Owner = this;
        
        if (ruleEditWindow.ShowDialog() == true && ruleEditWindow.Rule != null)
        {
            _viewModel.AddOrUpdateRule(ruleEditWindow.Rule, false);
        }
    }
    
    private void RulesDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (RulesDataGrid.SelectedItem is WatchRuleViewModel selectedRule)
        {
            EditRule(selectedRule);
        }
    }
    
    private void EditRuleMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (RulesDataGrid.SelectedItem is WatchRuleViewModel selectedRule)
        {
            EditRule(selectedRule);
        }
    }
    
    private void EditRule(WatchRuleViewModel ruleViewModel)
    {
        var ruleEditWindow = new RuleEditWindow(ruleViewModel.Rule);
        ruleEditWindow.Owner = this;
        
        if (ruleEditWindow.ShowDialog() == true)
        {
            _viewModel.AddOrUpdateRule(ruleViewModel.Rule, true);
        }
    }
    
    private void DeleteRuleMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (RulesDataGrid.SelectedItem is WatchRuleViewModel selectedRule)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete the rule for '{selectedRule.SourceFile}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _viewModel.DeleteRuleCommand.Execute(selectedRule);
            }
        }
    }
    
    private void RetryRuleMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (RulesDataGrid.SelectedItem is WatchRuleViewModel selectedRule)
        {
            _viewModel.RetryRuleCommand.Execute(selectedRule);
        }
    }
    
    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Are you sure you want to clear the activity log?",
            "Confirm Clear",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            _viewModel.ClearLogCommand.Execute(null);
        }
    }
    
    private void CopyLogEntryMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (ActivityLogListView.SelectedItem is LogEntryViewModel selectedLogEntry)
        {
            try
            {
                Clipboard.SetText(selectedLogEntry.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to copy to clipboard: {ex.Message}",
                    "Copy Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }
    
    // Settings
    private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Save polling speed setting from ComboBox
            _viewModel.PollingSpeedIndex = PollingSpeedComboBox.SelectedIndex;
            _viewModel.SaveSettings();
            
            MessageBox.Show("Settings saved successfully!", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void CancelSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        // Restore values from configuration
        LoadSettingsUI();
    }
    
    /// <summary>
    /// Loads settings into UI elements
    /// </summary>
    private void LoadSettingsUI()
    {
        PollingSpeedComboBox.SelectedIndex = _viewModel.PollingSpeedIndex;
    }
    
    /// <summary>
    /// Setup system tray icon
    /// </summary>
    private void SetupTrayIcon()
    {
        try
        {
            // Use PNG for tray icon to ensure transparency support
            var iconUri = new Uri("pack://application:,,,/Resources/Icons/app-icon-64.png");
            var streamResourceInfo = Application.GetResourceStream(iconUri);
            if (streamResourceInfo != null)
            {
                var bitmap = new System.Drawing.Bitmap(streamResourceInfo.Stream);
                // PNG already has correct transparency, no need to make transparent
                var icon = System.Drawing.Icon.FromHandle(bitmap.GetHicon());
                TrayIcon.Icon = icon;
                return;
            }
        }
        catch (Exception ex)
        {
            _logService?.Warning($"Failed to load PNG tray icon: {ex.Message}", nameof(MainWindow));
        }
        
        try
        {
            // Fallback to ICO file
            var iconUri = new Uri("pack://application:,,,/Resources/Icons/app-icon.ico");
            var streamResourceInfo = Application.GetResourceStream(iconUri);
            if (streamResourceInfo != null)
            {
                var icon = new System.Drawing.Icon(streamResourceInfo.Stream);
                TrayIcon.Icon = icon;
            }
        }
        catch (Exception ex)
        {
            // If failed to load icon, use default system icon
            _logService?.Warning($"Failed to load fallback tray icon: {ex.Message}", nameof(MainWindow));
        }
    }
    
    // System Tray handlers
    private void TrayIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
    {
        ShowWindow();
    }
    
    private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ShowWindow();
    }
    
    private async void PauseResumeMenuItem_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.ToggleMonitoringCommand.ExecuteAsync(null);
    }
    
    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.Application.Current.Shutdown();
    }
    
    private void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }
    
    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        // Hide to tray instead of closing
        e.Cancel = true;
        Hide();
    }
    
    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
        }
    }
}