namespace SkythornLauncher;

public static class LayoutMetrics
{
    public const double WindowWidth = 800;
    public const double WindowHeight = 600;

    private const double Scale = WindowWidth / 4032.0;

    public static double R(double designPixels) => Math.Round(designPixels * Scale, 1);

    // Menu buttons — Main Menu.pxz (4032×3024)
    // Layers: Play Button / Play Hover, Profiles Button / Profiles Hover, Settings Button / Settings Hover

    // Play Button (210,1031 954×285) / Play Hover (210,1039 954×292)
    // PXZ Y gap: 1039 - 1031 = 8 → scaled 8 * (800/4032) ≈ 1.6px
    public const double PlayButtonLeft = 42;
    public const double PlayButtonTop = 205;
    public const double PlayButtonWidth = 189;
    public const double PlayButtonHeight = 59;
    public const double PlayNormalTopOffset = 1;
    public const double PlayNormalHeight = 57;
    public const double PlayHoverTopOffset = 2.1;
    public const double PlayHoverHeight = 58;

    // 3 Buttons Border (189,1004 995×906) — drawn above button art
    public const double MenuButtonsBorderLeft = 37.5;
    public const double MenuButtonsBorderTop = 199.2;
    public const double MenuButtonsBorderWidth = 197.4;
    public const double MenuButtonsBorderHeight = 179.6;
    public const int MenuButtonArtZIndex = 2;
    public const int MenuButtonsBorderZIndex = 10;

    // Profiles Button (210,1316 954×285) / Profiles Hover (210,1316 954×292)
    public const double ProfileButtonLeft = 42;
    public const double ProfileButtonTop = 261;
    public const double ProfileButtonWidth = 189;
    public const double ProfileButtonHeight = 58;
    public const double ProfileNormalTopOffset = 0;
    public const double ProfileNormalHeight = 57;
    public const double ProfileHoverTopOffset = 0;
    public const double ProfileHoverHeight = 58;

    // Settings Button (210,1600 954×285) / Settings Hover (210,1600 954×292)
    public const double SettingsButtonLeft = 42;
    public const double SettingsButtonTop = 317;
    public const double SettingsButtonWidth = 189;
    public const double SettingsButtonHeight = 58;
    public const double SettingsNormalTopOffset = 0;
    public const double SettingsNormalHeight = 57;
    public const double SettingsHoverTopOffset = 0;
    public const double SettingsHoverHeight = 58;

    // Red panel — blue vertical marks after each colon
    public const double StatusValueLeft = 512;
    public const double StatusValueTop = 164;
    public const double FolderPathValueLeft = 562;
    public const double FolderPathValueTop = 269;
    public const double ProfileValueLeft = 519;
    public const double ProfileValueTop = 321;

    // Stats panel — blue vertical marks after each colon
    public const double ServerTimeValueLeft = 634;
    public const double ServerTimeValueTop = 421;
    public const double PlayersOnlineValueLeft = 661;
    public const double PlayersOnlineValueTop = 475;
    public const double ServerUptimeValueLeft = 647;
    public const double ServerUptimeValueTop = 523;

    // News — user-marked OK; unchanged
    public const double NewsValueLeft = 137;
    public const double NewsValueTop = 449;

    public const double LauncherVersionValueLeft = 286;
    public const double LauncherVersionValueTop = 568;
    public const double ServerVersionValueLeft = 671;
    public const double ServerVersionValueTop = 568;

    // Update notice — above launcher version line, no new menu art
    public const double UpdateNoticeLeft = 137;
    public const double UpdateNoticeTop = 548;
    public const double UpdateNoticeWidth = 420;

    public const double RedPanelValueWidth = 230;
    public const double StatsValueWidth = 150;
    public const double NewsValueWidth = 340;

    public const double DynamicFontSize = 16;
    public const double LauncherVersionFontSize = 14;
}
