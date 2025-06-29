using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace TinyFileSentry.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Настройка глобальной обработки исключений
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        
        // Применение системных цветов
        ApplySystemColors();
        
        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        
        // Стартуем скрытыми, если нужно
        if (e.Args.Any(arg => arg == "--start-hidden"))
        {
            mainWindow.WindowState = WindowState.Minimized;
            mainWindow.Hide();
        }
        else
        {
            mainWindow.Show();
        }
    }
    
    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"An unexpected error occurred:\n{e.Exception.Message}\n\nThe application will continue running.",
            "TinyFileSentry Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        
        e.Handled = true;
    }
    
    private void ApplySystemColors()
    {
        try
        {
            // Попытка получить системные цвета
            var accentColor = SystemColors.AccentColor;
            var windowColor = SystemColors.WindowColor;
            var windowTextColor = SystemColors.WindowTextColor;
            var controlColor = SystemColors.ControlColor;
            
            // Обновление ресурсов приложения
            if (Resources["SystemAccentBrush"] is SolidColorBrush accentBrush)
                accentBrush.Color = accentColor;
                
            if (Resources["BackgroundBrush"] is SolidColorBrush backgroundBrush)
                backgroundBrush.Color = windowColor;
                
            if (Resources["PrimaryTextBrush"] is SolidColorBrush textBrush)
                textBrush.Color = windowTextColor;
                
            if (Resources["SurfaceBrush"] is SolidColorBrush surfaceBrush)
                surfaceBrush.Color = controlColor;
        }
        catch
        {
            // Если системные цвета недоступны, используем fallback цвета
            // (они уже установлены в XAML)
        }
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        // Очистка ресурсов
        base.OnExit(e);
    }
}