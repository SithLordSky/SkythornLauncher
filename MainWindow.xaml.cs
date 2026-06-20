using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using SkythornLauncher.Models;
using SkythornLauncher.Services;

namespace SkythornLauncher;

public partial class MainWindow : Window
{
    private readonly ProfileStore _profileStore = new();
    private readonly ServerStatusService _serverStatus = new();
    private readonly NewsService _news = new();
    private readonly UpdateService _updates = new();
    private LauncherState _state = new();
    private bool _isLaunching;
    private Process? _gameProcess;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Closed += (_, _) =>
        {
            DetachGameProcessMonitor();
            _news.Dispose();
            _serverStatus.Dispose();
            _updates.Dispose();
        };
        _serverStatus.StatusUpdated += ApplyServerStatus;
        _news.NewsUpdated += ApplyNews;
        _updates.StatusUpdated += ApplyUpdateStatus;
        MouseLeftButtonDown += (_, e) =>
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        };
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await Dispatcher.Yield(DispatcherPriority.Background);

        try
        {
            _state = await Task.Run(() => _profileStore.Load());
            var active = FindActiveProfile();
            if (active.Preferences == null || IsEmptyPreferences(active.Preferences))
            {
                active.Preferences = SettingsWriter.ReadPreferences();
            }

            RefreshProfileDisplay(active);
            await Task.WhenAll(
                _serverStatus.RefreshAsync(),
                _news.RefreshAsync(),
                _updates.RefreshAsync());
        }
        catch (Exception ex)
        {
            StatusValueText.Text = "Error";
            MessageBox.Show(this, ex.Message, "Launcher", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void MonitorGameProcess(Process process)
    {
        DetachGameProcessMonitor();

        _gameProcess = process;
        GameProcessTracker.Track(process);
        if (process.HasExited)
        {
            return;
        }

        process.Exited += OnGameProcessExited;
    }

    private void DetachGameProcessMonitor()
    {
        if (_gameProcess == null)
        {
            GameProcessTracker.Clear();
            return;
        }

        _gameProcess.Exited -= OnGameProcessExited;
        _gameProcess = null;
        GameProcessTracker.Clear();
    }

    private void OnGameProcessExited(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(DetachGameProcessMonitor);
    }

    private static bool IsEmptyPreferences(ClientPreferences prefs)
    {
        return !prefs.AutoLogin && !prefs.AutoReconnect && !prefs.SaveAccount && prefs.MusicVolume == 50;
    }

    private LauncherProfile FindActiveProfile()
    {
        var profile = _state.Profiles.FirstOrDefault(p =>
            string.Equals(p.Name, _state.ActiveProfileName, StringComparison.OrdinalIgnoreCase));

        if (profile != null)
        {
            return profile;
        }

        if (_state.Profiles.Count > 0)
        {
            return _state.Profiles[0];
        }

        var created = new LauncherProfile();
        _state.Profiles.Add(created);
        _state.ActiveProfileName = created.Name;
        return created;
    }

    private void RefreshProfileDisplay(LauncherProfile profile)
    {
        ProfileValueText.Text = profile.Name;
        FolderPathValueText.Text = UiFormat.TruncatePath(profile.UltimaOnlineDirectory, 32);
        FolderPathValueText.ToolTip = profile.UltimaOnlineDirectory;
    }

    private void ApplyServerStatus(ServerStatusSnapshot snapshot)
    {
        Dispatcher.Invoke(() =>
        {
            StatusValueText.Text = UiFormat.StatusText(snapshot.Status);
            StatusOrb.Source = UiFormat.StatusIcon(snapshot.Status);
            ServerTimeValueText.Text = snapshot.EasternTime.ToString("h:mm tt") + " ET";
            PlayersOnlineValueText.Text = snapshot.PlayersOnline?.ToString() ?? "—";
            ServerUptimeValueText.Text = UiFormat.FormatUptime(snapshot.ServerUptime);
            ServerVersionValueText.Text = snapshot.ServerVersion ?? "—";
        });
    }

    private void ApplyNews(NewsSnapshot snapshot)
    {
        Dispatcher.Invoke(() => RenderNews(snapshot));
    }

    private void ApplyUpdateStatus(UpdateSnapshot snapshot)
    {
        Dispatcher.Invoke(() =>
        {
            UpdateNoticeText.Visibility = snapshot.State == UpdateCheckState.UpdateAvailable
                ? Visibility.Visible
                : Visibility.Collapsed;
        });
    }

    private void UpdateNotice_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        OpenSettings();
    }

    private void RenderNews(NewsSnapshot snapshot)
    {
        NewsPanel.Children.Clear();

        if (snapshot.IsLoading)
        {
            NewsPanel.Children.Add(CreateNewsMessage("Loading news..."));
            return;
        }

        if (snapshot.Failed || snapshot.Posts.Count == 0)
        {
            var fallback = CreateNewsMessage("Unable to load news. Visit the website for updates.");
            fallback.MouseLeftButtonDown += (_, _) => OpenWebsite();
            fallback.Cursor = Cursors.Hand;
            fallback.ToolTip = LauncherConstants.WebsiteUrl;
            NewsPanel.Children.Add(fallback);
            return;
        }

        for (var i = 0; i < snapshot.Posts.Count; i++)
        {
            if (i > 0)
            {
                NewsPanel.Children.Add(new Border
                {
                    Height = 6,
                    Background = Brushes.Transparent
                });
            }

            NewsPanel.Children.Add(CreateNewsItem(snapshot.Posts[i]));
        }
    }

    private static TextBlock CreateNewsMessage(string text)
    {
        return new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(0xF2, 0xF2, 0xF2)),
            FontFamily = Application.Current.TryFindResource("FantasyFont") as FontFamily,
            FontSize = 14
        };
    }

    private static UIElement CreateNewsItem(BlogPostItem post)
    {
        var panel = new StackPanel { Cursor = Cursors.Hand, ToolTip = post.Url };

        var title = new TextBlock
        {
            Text = post.Title,
            TextWrapping = TextWrapping.Wrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxHeight = 20,
            Foreground = new SolidColorBrush(Color.FromRgb(0xF7, 0xE8, 0xA8)),
            FontFamily = Application.Current.TryFindResource("FantasyFont") as FontFamily,
            FontSize = 14,
            FontWeight = FontWeights.Bold
        };

        var date = new TextBlock
        {
            Text = UiFormat.FormatNewsDate(post.PublishedDate),
            Foreground = new SolidColorBrush(Color.FromRgb(0xC8, 0xC8, 0xC8)),
            FontFamily = Application.Current.TryFindResource("FantasyFont") as FontFamily,
            FontSize = 12,
            Margin = new Thickness(0, 1, 0, 2)
        };

        var excerpt = new TextBlock
        {
            Text = UiFormat.TrimExcerpt(post.Excerpt),
            TextWrapping = TextWrapping.Wrap,
            MaxHeight = 34,
            Foreground = new SolidColorBrush(Color.FromRgb(0xF2, 0xF2, 0xF2)),
            FontFamily = Application.Current.TryFindResource("FantasyFont") as FontFamily,
            FontSize = 13
        };

        panel.Children.Add(title);
        if (!string.IsNullOrWhiteSpace(date.Text))
        {
            panel.Children.Add(date);
        }

        panel.Children.Add(excerpt);

        panel.MouseLeftButtonDown += (_, e) =>
        {
            OpenUrl(post.Url);
            e.Handled = true;
        };

        return panel;
    }

    private static void OpenWebsite() => OpenUrl(LauncherConstants.WebsiteUrl);

    private static void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // Ignore browser launch failures; launcher stays usable.
        }
    }

    private void Profile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ProfileWindow(_state, FindActiveProfile())
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true && dialog.ResultState != null)
        {
            _state = dialog.ResultState;
            _profileStore.Save(_state);
            RefreshProfileDisplay(FindActiveProfile());
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e) => OpenSettings();

    private void OpenSettings()
    {
        var profile = FindActiveProfile();
        var dialog = new SettingsWindow(profile, _updates)
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true && dialog.ResultProfile != null)
        {
            var index = _state.Profiles.FindIndex(p =>
                string.Equals(p.Name, profile.Name, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                _state.Profiles[index] = dialog.ResultProfile;
            }

            _profileStore.Save(_state);
            SettingsWriter.Write(dialog.ResultProfile);
            RefreshProfileDisplay(FindActiveProfile());
        }
    }

    private async void Play_Click(object sender, RoutedEventArgs e)
    {
        if (_isLaunching)
        {
            return;
        }

        try
        {
            _isLaunching = true;
            PlayButton.IsEnabled = false;
            StatusValueText.Text = "Launching...";

            var profile = FindActiveProfile();
            profile.LastUsedUtc = DateTime.UtcNow;

            var password = profile.HasSavedPassword
                ? SecretProtector.Unprotect(profile.PasswordProtected)
                : string.Empty;

            if (profile.Preferences.AutoLoginOnPlay)
            {
                profile.Preferences.AutoLogin = true;
            }

            SettingsWriter.Write(profile, password);
            var result = await GameLauncher.LaunchAsync(profile, password);

            if (!string.IsNullOrWhiteSpace(result.EarlyError))
            {
                MessageBox.Show(
                    this,
                    result.EarlyError + Environment.NewLine + Environment.NewLine +
                    $"Details: {LauncherConstants.LaunchLogPath}",
                    "Launch Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                DetachGameProcessMonitor();
            }
            else if (result.Process.HasExited)
            {
                DetachGameProcessMonitor();
            }
            else
            {
                MonitorGameProcess(result.Process);
            }

            _profileStore.Save(_state);
            ApplyServerStatus(_serverStatus.Latest);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Launch Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            DetachGameProcessMonitor();
        }
        finally
        {
            _isLaunching = false;
            PlayButton.IsEnabled = true;
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
