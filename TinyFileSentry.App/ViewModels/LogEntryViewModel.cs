using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using TinyFileSentry.Core.Models;

namespace TinyFileSentry.App.ViewModels;

/// <summary>
/// ViewModel для записи в логе активности
/// </summary>
public class LogEntryViewModel : INotifyPropertyChanged
{
    private readonly LogEntry _logEntry;
    
    public LogEntryViewModel(LogEntry logEntry)
    {
        _logEntry = logEntry ?? throw new ArgumentNullException(nameof(logEntry));
    }
    
    public LogEntry LogEntry => _logEntry;
    
    public DateTime Timestamp => _logEntry.Timestamp;
    
    public LogLevel Level => _logEntry.Level;
    
    public string Message => _logEntry.Message;
    
    public Brush LevelColor => Level switch
    {
        LogLevel.Info => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")!),
        LogLevel.Warning => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800")!),
        LogLevel.Error => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336")!),
        _ => new SolidColorBrush(Colors.Gray)
    };
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}