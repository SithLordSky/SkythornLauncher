using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using SkythornLauncher.Models;
using SkythornLauncher.Services;

namespace SkythornLauncher;

public partial class MainWindow : Window
{
    private readonly ProfileStore _profileStore = new();
    private readonly ServerStatusService _serverStatus = new();
    private LauncherState _state = new();
    private bool _isLaunching;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Closed += (_, _) => _serverStatus.Dispose();
        _serverStatus.StatusUpdated += ApplyServerStatus;
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
            await _serverStatus.RefreshAsync();
        }
        catch (Exception ex)
        {
            StatusValueText.Text = "Error";
            MessageBox.Show(this, ex.Message, "Launcher", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
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
        });
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

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var profile = FindActiveProfile();
        var dialog = new SettingsWindow(profile)
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
            }

            _profileStore.Save(_state);
            ApplyServerStatus(_serverStatus.Latest);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Launch Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            _isLaunching = false;
            PlayButton.IsEnabled = true;
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
