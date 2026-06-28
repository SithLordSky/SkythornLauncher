using System.Windows;
using System.Windows.Controls;
using SkythornLauncher.Models;
using SkythornLauncher.Services;

namespace SkythornLauncher;

public partial class ProfileWindow : Window
{
    private LauncherProfile? _selectedProfile;

    public LauncherState? ResultState { get; private set; }

    public ProfileWindow(LauncherState state, LauncherProfile activeProfile)
    {
        InitializeComponent();
        SubWindowChrome.EnableDragMove(this);
        ResultState = CloneState(state);
        RefreshList(activeProfile.Name);
    }

    private static LauncherState CloneState(LauncherState state)
    {
        return new LauncherState
        {
            ActiveProfileName = state.ActiveProfileName,
            Profiles = state.Profiles.Select(p => new LauncherProfile
            {
                Name = p.Name,
                Username = p.Username,
                PasswordProtected = p.PasswordProtected,
                UltimaOnlineDirectory = p.UltimaOnlineDirectory,
                SaveAccount = p.SaveAccount,
                LastUsedUtc = p.LastUsedUtc,
                Preferences = new ClientPreferences
                {
                    AutoLogin = p.Preferences.AutoLogin,
                    AutoLoginOnPlay = p.Preferences.AutoLoginOnPlay,
                    SkipLoginScreen = p.Preferences.SkipLoginScreen,
                    AutoReconnect = p.Preferences.AutoReconnect,
                    ReconnectDelayMs = p.Preferences.ReconnectDelayMs,
                    SaveAccount = p.Preferences.SaveAccount,
                    EnablePacketLog = p.Preferences.EnablePacketLog,
                    EnableMusic = p.Preferences.EnableMusic,
                    HighDpi = p.Preferences.HighDpi,
                    MusicVolume = p.Preferences.MusicVolume,
                    ForceDriver = p.Preferences.ForceDriver
                }
            }).ToList()
        };
    }

    private void RefreshList(string? selectName = null)
    {
        if (ResultState == null)
        {
            return;
        }

        ProfileList.ItemsSource = null;
        ProfileList.ItemsSource = ResultState.Profiles.Select(p => p.Name).ToList();

        var index = ResultState.Profiles.FindIndex(p =>
            string.Equals(p.Name, selectName ?? ResultState.ActiveProfileName, StringComparison.OrdinalIgnoreCase));

        ProfileList.SelectedIndex = index >= 0 ? index : 0;
    }

    private void ProfileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ResultState == null || ProfileList.SelectedIndex < 0)
        {
            return;
        }

        _selectedProfile = ResultState.Profiles[ProfileList.SelectedIndex];
        ProfileNameBox.Text = _selectedProfile.Name;
        UsernameBox.Text = _selectedProfile.Username;
        PasswordBox.Password = _selectedProfile.HasSavedPassword
            ? SecretProtector.Unprotect(_selectedProfile.PasswordProtected)
            : string.Empty;
        SkipLoginScreenBox.IsChecked = _selectedProfile.Preferences.SkipLoginScreen;
        AutoLoginBox.IsChecked = _selectedProfile.Preferences.AutoLogin;
        SaveAccountBox.IsChecked = _selectedProfile.SaveAccount || _selectedProfile.Preferences.SaveAccount;
    }

    private void New_Click(object sender, RoutedEventArgs e)
    {
        if (ResultState == null)
        {
            return;
        }

        var index = 1;
        var name = "Profile 1";
        while (ResultState.Profiles.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            index++;
            name = $"Profile {index}";
        }

        var profile = new LauncherProfile { Name = name };
        ResultState.Profiles.Add(profile);
        RefreshList(name);
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (ResultState == null || _selectedProfile == null || ResultState.Profiles.Count <= 1)
        {
            MessageBox.Show(this, "At least one profile is required.", "Profiles", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show(this, $"Delete profile '{_selectedProfile.Name}'?", "Profiles", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        ResultState.Profiles.Remove(_selectedProfile);
        ResultState.ActiveProfileName = ResultState.Profiles[0].Name;
        RefreshList();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (ResultState == null || _selectedProfile == null)
        {
            return;
        }

        var newName = ProfileNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(newName))
        {
            MessageBox.Show(this, "Profile name cannot be empty.", "Profiles", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var oldName = _selectedProfile.Name;
        if (!string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
        {
            if (ResultState.Profiles.Any(p =>
                    !ReferenceEquals(p, _selectedProfile) &&
                    string.Equals(p.Name, newName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show(this, $"A profile named '{newName}' already exists.", "Profiles", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.Equals(ResultState.ActiveProfileName, oldName, StringComparison.OrdinalIgnoreCase))
            {
                ResultState.ActiveProfileName = newName;
            }

            _selectedProfile.Name = newName;
        }

        _selectedProfile.Username = UsernameBox.Text.Trim();
        _selectedProfile.SaveAccount = SaveAccountBox.IsChecked == true;
        _selectedProfile.Preferences.SkipLoginScreen = SkipLoginScreenBox.IsChecked == true;
        _selectedProfile.Preferences.AutoLogin = AutoLoginBox.IsChecked == true;
        _selectedProfile.Preferences.SaveAccount = SaveAccountBox.IsChecked == true;

        var password = PasswordBox.Password;
        if (_selectedProfile.SaveAccount && !string.IsNullOrEmpty(password))
        {
            _selectedProfile.PasswordProtected = SecretProtector.Protect(password);
        }
        else if (!_selectedProfile.SaveAccount)
        {
            _selectedProfile.PasswordProtected = string.Empty;
        }

        ResultState.ActiveProfileName = _selectedProfile.Name;
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
