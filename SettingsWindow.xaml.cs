using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SkythornLauncher.Models;
using SkythornLauncher.Services;

namespace SkythornLauncher;

public partial class SettingsWindow : Window
{
    private readonly LauncherProfile _profile;

    public LauncherProfile? ResultProfile { get; private set; }

    public SettingsWindow(LauncherProfile profile)
    {
        InitializeComponent();
        SubWindowChrome.EnableDragMove(this);
        _profile = profile;
        LoadFromProfile();
    }

    private void LoadFromProfile()
    {
        var prefs = _profile.Preferences;
        SkipLoginScreenBox.IsChecked = prefs.SkipLoginScreen;
        AutoLoginBox.IsChecked = prefs.AutoLogin;
        AutoReconnectBox.IsChecked = prefs.AutoReconnect;
        SaveAccountBox.IsChecked = prefs.SaveAccount || _profile.SaveAccount;
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
            SaveAccount = SaveAccountBox.IsChecked == true,
            LastUsedUtc = _profile.LastUsedUtc,
            Preferences = new ClientPreferences
            {
                SkipLoginScreen = SkipLoginScreenBox.IsChecked == true,
                AutoLogin = AutoLoginBox.IsChecked == true,
                AutoReconnect = AutoReconnectBox.IsChecked == true,
                ReconnectDelayMs = reconnectMs,
                SaveAccount = SaveAccountBox.IsChecked == true,
                EnablePacketLog = PacketLogBox.IsChecked == true,
                EnableMusic = MusicBox.IsChecked == true,
                HighDpi = HighDpiBox.IsChecked == true,
                MusicVolume = (int)MusicVolumeSlider.Value,
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

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
