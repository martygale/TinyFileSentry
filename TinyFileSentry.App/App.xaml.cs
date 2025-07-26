using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace TinyFileSentry.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Setup global exception handling
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        
        // Apply system colors
        ApplySystemColors();
        
        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        
        // Start hidden if needed
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
            // Try to get system colors
            var accentColor = SystemColors.AccentColor;
            var windowColor = SystemColors.WindowColor;
            var windowTextColor = SystemColors.WindowTextColor;
            var controlColor = SystemColors.ControlColor;
            
            // Update application resources
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
            // If system colors are unavailable, use fallback colors
            // (they are already set in XAML)
        }
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        // Resource cleanup
        base.OnExit(e);
    }
}