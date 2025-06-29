using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using TinyFileSentry.Core.Models;

namespace TinyFileSentry.App.ViewModels;

/// <summary>
/// ViewModel for displaying monitoring rule in the table
/// </summary>
public class WatchRuleViewModel : INotifyPropertyChanged
{
    private readonly WatchRule _rule;
    private RuleStatus _status = RuleStatus.Synchronized;
    private DateTime? _lastActionTime;
    
    public WatchRuleViewModel(WatchRule rule)
    {
        _rule = rule ?? throw new ArgumentNullException(nameof(rule));
    }
    
    public WatchRule Rule => _rule;
    
    public string SourceFile
    {
        get => _rule.SourceFile;
        set
        {
            if (_rule.SourceFile != value)
            {
                _rule.SourceFile = value;
                OnPropertyChanged();
            }
        }
    }
    
    public string DestinationRoot
    {
        get => _rule.DestinationRoot;
        set
        {
            if (_rule.DestinationRoot != value)
            {
                _rule.DestinationRoot = value;
                OnPropertyChanged();
            }
        }
    }
    
    public PostActionType PostAction
    {
        get => _rule.PostAction;
        set
        {
            if (_rule.PostAction != value)
            {
                _rule.PostAction = value;
                OnPropertyChanged();
            }
        }
    }
    
    public bool IsEnabled
    {
        get => _rule.IsEnabled;
        set
        {
            if (_rule.IsEnabled != value)
            {
                _rule.IsEnabled = value;
                OnPropertyChanged();
            }
        }
    }
    
    public RuleStatus Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(StatusIcon));
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(HasError));
            }
        }
    }
    
    public DateTime? LastActionTime
    {
        get => _lastActionTime;
        set
        {
            if (_lastActionTime != value)
            {
                _lastActionTime = value;
                OnPropertyChanged();
            }
        }
    }
    
    public string StatusText => Status switch
    {
        RuleStatus.Synchronized => "Synchronized",
        RuleStatus.Copying => "Copying",
        RuleStatus.Error => "Error",
        RuleStatus.SourceDeleted => "Source Deleted",
        _ => "Unknown"
    };
    
    public string StatusIcon => Status switch
    {
        RuleStatus.Synchronized => "SuccessIcon",
        RuleStatus.Copying => "PlayIcon",
        RuleStatus.Error => "ErrorIcon",
        RuleStatus.SourceDeleted => "ErrorIcon",
        _ => "WaitingIcon"
    };
    
    public Brush StatusColor => Status switch
    {
        RuleStatus.Synchronized => new SolidColorBrush(Colors.Green),
        RuleStatus.Copying => new SolidColorBrush(Colors.Blue),
        RuleStatus.Error => new SolidColorBrush(Colors.Red),
        RuleStatus.SourceDeleted => new SolidColorBrush(Colors.Red),
        _ => new SolidColorBrush(Colors.Gray)
    };
    
    public bool HasError => Status == RuleStatus.Error;
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}