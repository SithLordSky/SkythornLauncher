using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SkythornLauncher.Models;
using SkythornLauncher.Services;

namespace SkythornLauncher;

public partial class SettingsWindow : Window
{
    private readonly LauncherProfile _profile;
    private readonly UpdateService _updates;

    public LauncherProfile? ResultProfile { get; private set; }

    public SettingsWindow(LauncherProfile profile, UpdateService updates)
    {
        InitializeComponent();
        SubWindowChrome.EnableDragMove(this);
        _profile = profile;
        _updates = updates;
        _updates.StatusUpdated += ApplyUpdateStatus;
        Closed += (_, _) => _updates.StatusUpdated -= ApplyUpdateStatus;
        LoadFromProfile();
        ApplyUpdateStatus(_updates.Latest);
        Loaded += SettingsWindow_Loaded;
    }

    private async void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _updates.RefreshAsync();
    }

    private void LoadFromProfile()
    {
        var prefs = _profile.Preferences;
        AutoReconnectBox.IsChecked = prefs.AutoReconnect;
        PacketLogBox.IsChecked = prefs.EnablePacketLog;
        MusicBox.IsChecked = prefs.EnableMusic;
        HighDpiBox.IsChecked = prefs.HighDpi;
        ReconnectDelayBox.Text = prefs.ReconnectDelayMs.ToString();
        MusicVolumeSlider.Value = prefs.MusicVolume;
        GamePathBox.Text = _profile.UltimaOnlineDirectory;

        foreach (ComboBoxItem item in DriverBox.Items)
        {
            if (item.Tag?.ToString() == prefs.ForceDriver.ToString())
            {
                DriverBox.SelectedItem = item;
                break;
            }
        }

        if (DriverBox.SelectedIndex < 0)
        {
            DriverBox.SelectedIndex = 0;
        }
    }

    private void ApplyUpdateStatus(UpdateSnapshot snapshot)
    {
        Dispatcher.Invoke(() =>
        {
            UpdateCurrentVersionText.Text = snapshot.CurrentVersion;
            UpdateLatestVersionText.Text = string.IsNullOrWhiteSpace(snapshot.LatestVersion) ? "—" : snapshot.LatestVersion;
            UpdateStatusText.Text = FormatUpdateStatus(snapshot);
            CheckForUpdatesButton.IsEnabled = !snapshot.IsBusy;
            InstallUpdateButton.IsEnabled =
                snapshot.State == UpdateCheckState.UpdateAvailable &&
                !snapshot.IsBusy &&
                !GameProcessTracker.IsGameRunning();
        });
    }

    private static string FormatUpdateStatus(UpdateSnapshot snapshot)
    {
        if (snapshot.State == UpdateCheckState.UpdateAvailable &&
            GameProcessTracker.IsGameRunning())
        {
            return "Update available — please close the game before updating.";
        }

        var baseText = snapshot.State switch
        {
            UpdateCheckState.Checking => "Checking...",
            UpdateCheckState.UpToDate => "Up to date",
            UpdateCheckState.UpdateAvailable =>
                snapshot.OutdatedFileCount > 0
                    ? $"Update available ({snapshot.OutdatedFileCount} file(s) out of date)"
                    : "Update available",
            UpdateCheckState.CheckFailed => "Unable to check for updates",
            UpdateCheckState.Downloading => "Downloading...",
            UpdateCheckState.Verifying => "Verifying...",
            UpdateCheckState.RestartingLauncher => "Restarting launcher...",
            UpdateCheckState.UpdateFailed => "Update failed",
            _ => "—"
        };

        if (!string.IsNullOrWhiteSpace(snapshot.ErrorMessage) &&
            snapshot.State is UpdateCheckState.CheckFailed or UpdateCheckState.UpdateFailed)
        {
            return $"{baseText}: {snapshot.ErrorMessage}";
        }

        if (snapshot.State == UpdateCheckState.UpdateAvailable &&
            snapshot.OutdatedPaths.Count > 0)
        {
            var listed = string.Join(", ", snapshot.OutdatedPaths.Take(4));
            if (snapshot.OutdatedPaths.Count > 4)
            {
                listed += $", +{snapshot.OutdatedPaths.Count - 4} more";
            }

            return $"{baseText}: {listed}";
        }

        return baseText;
    }

    private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
    {
        CheckForUpdatesButton.IsEnabled = false;
        await _updates.RefreshAsync();
    }

    private async void InstallUpdate_Click(object sender, RoutedEventArgs e)
    {
        if (GameProcessTracker.IsGameRunning())
        {
            MessageBox.Show(
                this,
                "Please close the game before updating.",
                "Updates",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        InstallUpdateButton.IsEnabled = false;
        CheckForUpdatesButton.IsEnabled = false;
        await _updates.InstallUpdateAsync();

        if (_updates.Latest.State == UpdateCheckState.UpdateFailed &&
            !string.IsNullOrWhiteSpace(_updates.Latest.ErrorMessage))
        {
            MessageBox.Show(
                this,
                _updates.Latest.ErrorMessage,
                "Update Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Ultima Online client files folder",
            InitialDirectory = Directory.Exists(GamePathBox.Text)
                ? GamePathBox.Text
                : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
        };

        if (dialog.ShowDialog() == true)
        {
            GamePathBox.Text = dialog.FolderName;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(ReconnectDelayBox.Text.Trim(), out var reconnectMs) || reconnectMs < 1000)
        {
            MessageBox.Show(this, "Reconnect delay must be at least 1000 ms.", "Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var driverItem = (ComboBoxItem?)DriverBox.SelectedItem;
        byte driver = byte.TryParse(driverItem?.Tag?.ToString(), out var d) ? d : (byte)0;

        var updated = new LauncherProfile
        {
            Name = _profile.Name,
            Username = _profile.Username,
            PasswordProtected = _profile.PasswordProtected,
            UltimaOnlineDirectory = GamePathBox.Text.Trim(),
            SaveAccount = _profile.SaveAccount,
            LastUsedUtc = _profile.LastUsedUtc,
            Preferences = new ClientPreferences
            {
                SkipLoginScreen = _profile.Preferences.SkipLoginScreen,
                AutoLogin = _profile.Preferences.AutoLogin,
                AutoReconnect = AutoReconnectBox.IsChecked == true,
                ReconnectDelayMs = reconnectMs,
                SaveAccount = _profile.Preferences.SaveAccount,
                EnablePacketLog = PacketLogBox.IsChecked == true,
                EnableMusic = MusicBox.IsChecked == true,
                HighDpi = HighDpiBox.IsChecked == true,
                MusicVolume = (int)MusicVolumeSlider.Value,
                FootstepsVolume = _profile.Preferences.FootstepsVolume,
                ForceDriver = driver
            }
        };

        SettingsWriter.Write(updated);
        ResultProfile = updated;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
