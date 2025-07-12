using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TinyFileSentry.Core.Extensions;
using TinyFileSentry.Core.Interfaces;
using TinyFileSentry.Core.Models;
using TinyFileSentry.Core.Services;

namespace TinyFileSentry.App.ViewModels;

/// <summary>
/// Main ViewModel for the application's main window
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IPollerService _pollerService;
    private readonly IRulesService _rulesService;
    private readonly ILogService _logService;
    private readonly IConfigurationService _configurationService;
    
    [ObservableProperty]
    private bool _isMonitoringActive = false;
    
    [ObservableProperty]
    private string _searchText = string.Empty;
    
    [ObservableProperty]
    private bool _showErrorsOnly = false;
    
    [ObservableProperty]
    private bool _showInfo = true;
    
    [ObservableProperty]
    private bool _showWarning = true;
    
    [ObservableProperty]
    private bool _showError = true;
    
    [ObservableProperty]
    private int _pollingSpeedIndex = 2; // Default to Fast (index 2)
    
    public ObservableCollection<WatchRuleViewModel> WatchRules { get; } = new();
    public ObservableCollection<LogEntryViewModel> LogEntries { get; } = new();
    
    private readonly ICollectionView _filteredRules;
    private readonly ICollectionView _filteredLogEntries;
    
    public MainViewModel(
        IPollerService pollerService,
        IRulesService rulesService,
        ILogService logService,
        IConfigurationService configurationService)
    {
        _pollerService = pollerService ?? throw new ArgumentNullException(nameof(pollerService));
        _rulesService = rulesService ?? throw new ArgumentNullException(nameof(rulesService));
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        
        // Setup filtering for rules
        _filteredRules = CollectionViewSource.GetDefaultView(WatchRules);
        _filteredRules.Filter = FilterRules;
        
        // Setup filtering for logs
        _filteredLogEntries = CollectionViewSource.GetDefaultView(LogEntries);
        _filteredLogEntries.Filter = FilterLogEntries;
        
        // Load data
        LoadWatchRules();
        LoadLogEntries();
        LoadSettings();
        
        // Auto-start monitoring if enabled in configuration
        if (IsMonitoringActive)
        {
            _ = Task.Run(async () => await _pollerService.StartAsync());
        }
        
        // Subscribe to search and filter changes
        PropertyChanged += OnFilterPropertyChanged;
        
        // Subscribe to service events
        _pollerService.FileChanged += OnFileChanged;
        _pollerService.RuleStatusChanged += OnRuleStatusChanged;
        _logService.LogAdded += OnLogEntryAdded;
    }
    
    private void OnFilterPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SearchText) || e.PropertyName == nameof(ShowErrorsOnly))
        {
            _filteredRules.Refresh();
        }
        
        if (e.PropertyName == nameof(ShowInfo) || 
            e.PropertyName == nameof(ShowWarning) || 
            e.PropertyName == nameof(ShowError))
        {
            _filteredLogEntries.Refresh();
        }
    }
    
    private bool FilterRules(object item)
    {
        if (item is not WatchRuleViewModel rule) return false;
        
        // Filter by errors
        if (ShowErrorsOnly && !rule.HasError)
            return false;
        
        // Text search
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLowerInvariant();
            if (!rule.SourceFile.ToLowerInvariant().Contains(searchLower) &&
                !rule.DestinationRoot.ToLowerInvariant().Contains(searchLower))
            {
                return false;
            }
        }
        
        return true;
    }
    
    private bool FilterLogEntries(object item)
    {
        if (item is not LogEntryViewModel logEntry) return false;
        
        return logEntry.Level switch
        {
            LogLevel.Info => ShowInfo,
            LogLevel.Warning => ShowWarning,
            LogLevel.Error => ShowError,
            _ => true
        };
    }
    
    private void LoadWatchRules()
    {
        var rules = _rulesService.GetWatchRules();
        WatchRules.Clear();
        
        foreach (var rule in rules)
        {
            WatchRules.Add(new WatchRuleViewModel(rule));
        }
    }
    
    private void LoadLogEntries()
    {
        var entries = _logService.GetLogs();
        LogEntries.Clear();
        
        foreach (var entry in entries.OrderByDescending(e => e.Timestamp).Take(1000))
        {
            LogEntries.Insert(0, new LogEntryViewModel(entry));
        }
    }
    
    [RelayCommand]
    private async Task ToggleMonitoring()
    {
        if (IsMonitoringActive)
        {
            await _pollerService.StopAsync();
            IsMonitoringActive = false;
        }
        else
        {
            await _pollerService.StartAsync();
            IsMonitoringActive = true;
        }
        
        // Save monitoring state to configuration
        var config = _configurationService.LoadConfiguration();
        config.IsMonitoringActive = IsMonitoringActive;
        _configurationService.SaveConfiguration(config);
    }
    
    [RelayCommand]
    private void AddRule()
    {
        // Command will be called from View to open modal window
    }
    
    [RelayCommand]
    private void EditRule(WatchRuleViewModel? ruleViewModel)
    {
        // Command will be called from View to open modal window
    }
    
    [RelayCommand]
    private void DeleteRule(WatchRuleViewModel? ruleViewModel)
    {
        if (ruleViewModel == null) return;
        
        _rulesService.RemoveWatchRule(ruleViewModel.Rule);
        WatchRules.Remove(ruleViewModel);
        
        // Save configuration
        var config = _configurationService.LoadConfiguration();
        config.WatchRules = WatchRules.Select(r => r.Rule).ToList();
        _configurationService.SaveConfiguration(config);
    }
    
    [RelayCommand]
    private void RetryRule(WatchRuleViewModel? ruleViewModel)
    {
        if (ruleViewModel == null || !ruleViewModel.HasError) return;
        
        // Force rule check
        ruleViewModel.Status = RuleStatus.Synchronized;
        // TODO: Триггер проверки через PollerService
    }
    
    [RelayCommand]
    private void ClearLog()
    {
        // Since there's no Clear method in the interface, just clear the UI
        LogEntries.Clear();
    }
    
    /// <summary>
    /// Loads settings from configuration
    /// </summary>
    private void LoadSettings()
    {
        var config = _configurationService.LoadConfiguration();
        PollingSpeedIndex = config.PollingSpeed.ToComboBoxIndex();
        IsMonitoringActive = config.IsMonitoringActive;
    }
    
    /// <summary>
    /// Saves settings to configuration
    /// </summary>
    public void SaveSettings()
    {
        var config = _configurationService.LoadConfiguration();
        config.PollingSpeed = PollingSpeedExtensions.FromComboBoxIndex(PollingSpeedIndex);
        config.IsMonitoringActive = IsMonitoringActive;
        _configurationService.SaveConfiguration(config);
    }
    
    public void AddOrUpdateRule(WatchRule rule, bool isEdit = false)
    {
        if (isEdit)
        {
            // Завершить редактирование в CollectionView перед обновлением
            if (_filteredRules is IEditableCollectionView editableView && editableView.IsEditingItem)
            {
                editableView.CommitEdit();
            }
            
            // Update existing rule - объект уже обновлен через DataBinding
            // Обновления ViewModel не нужны, так как Rule обновился напрямую
        }
        else
        {
            // Add new rule
            _rulesService.AddWatchRule(rule);
            WatchRules.Add(new WatchRuleViewModel(rule));
        }
        
        // Save configuration
        var config = _configurationService.LoadConfiguration();
        config.WatchRules = WatchRules.Select(r => r.Rule).ToList();
        _configurationService.SaveConfiguration(config);
        
        // Refresh только для новых правил, для редактирования не нужно
        if (!isEdit)
        {
            _filteredRules.Refresh();
        }
    }
    
    // Service event handlers
    private void OnFileChanged(object? sender, string filePath)
    {
        var ruleViewModel = WatchRules.FirstOrDefault(r => r.SourceFile == filePath);
        if (ruleViewModel != null)
        {
            ruleViewModel.Status = RuleStatus.Copying;
        }
    }
    
    private void OnRuleStatusChanged(object? sender, (string FilePath, RuleStatus Status) args)
    {
        System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
        {
            var ruleViewModel = WatchRules.FirstOrDefault(r => r.SourceFile == args.FilePath);
            if (ruleViewModel != null)
            {
                ruleViewModel.Status = args.Status;
            }
        });
    }
    
    private void OnLogEntryAdded(object? sender, LogEntry logEntry)
    {
        System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
        {
            LogEntries.Insert(0, new LogEntryViewModel(logEntry));
            
            // Limit number of entries in UI (last 1000)
            while (LogEntries.Count > 1000)
            {
                LogEntries.RemoveAt(LogEntries.Count - 1);
            }
        });
    }
}