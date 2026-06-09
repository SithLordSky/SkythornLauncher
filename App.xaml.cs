using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace SkythornLauncher;

public partial class App : Application
{
    public const string FantasyFontFamilyName = "Harrington";

    public App()
    {
        InitializeComponent();
        EnsureBundledFont();
    }

    private Cursor? _launcherCursor;
    private ImageSource? _appIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            LogCrash(args.Exception);
            MessageBox.Show(
                args.Exception.Message,
                "Launcher Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
            Shutdown(1);
        };

        base.OnStartup(e);

        _appIcon = LauncherAppIcon.Load();
        _launcherCursor = LauncherCursor.TryCreate();

        EventManager.RegisterClassHandler(
            typeof(Window),
            Window.LoadedEvent,
            new RoutedEventHandler(OnWindowLoaded));

        if (_launcherCursor != null)
        {
            EventManager.RegisterClassHandler(
                typeof(System.Windows.Controls.Button),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnInteractiveElementLoadedSetCursor));
        }
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Window window)
        {
            return;
        }

        if (_appIcon != null)
        {
            window.Icon = _appIcon;
        }

        if (_launcherCursor != null)
        {
            window.Cursor = _launcherCursor;
        }
    }

    private void OnInteractiveElementLoadedSetCursor(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && _launcherCursor != null)
        {
            element.Cursor = _launcherCursor;
        }
    }

    private void EnsureBundledFont()
    {
        var fontPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "Harrington.ttf");
        if (!File.Exists(fontPath))
        {
            return;
        }

        var fontUri = new Uri(fontPath, UriKind.Absolute);
        Resources["FantasyFont"] = new FontFamily(fontUri, $"./#{FantasyFontFamilyName}");
    }

    private static void LogCrash(Exception ex)
    {
        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                LauncherConstants.AppDataFolderName,
                "Logs");
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, $"launcher-crash-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
            File.WriteAllText(logPath, ex.ToString());
        }
        catch
        {
            // ignore logging failures
        }
    }
}
