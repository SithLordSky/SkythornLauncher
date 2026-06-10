namespace SkythornLauncher;

/// <summary>Button placement from Profiles Menu.pxz and Settings Menu.pxz (4032×3024).</summary>
public static class SubWindowLayoutMetrics
{
    public const int SubWindowButtonArtZIndex = 10;

    // New (611,2510 358×238) / hover (631,2530 322×196)
    public static double ProfilesNewButtonLeft => LayoutMetrics.R(611);
    public static double ProfilesNewButtonTop => LayoutMetrics.R(2510);
    public static double ProfilesNewButtonWidth => LayoutMetrics.R(358);
    public static double ProfilesNewButtonHeight => LayoutMetrics.R(238);
    public static double ProfilesNewHoverLeftOffset => LayoutMetrics.R(20);
    public static double ProfilesNewHoverTopOffset => LayoutMetrics.R(20);
    public static double ProfilesNewHoverWidth => LayoutMetrics.R(322);
    public static double ProfilesNewHoverHeight => LayoutMetrics.R(196);

    // Delete (969,2510 358×238) / hover (989,2529 321×199)
    public static double ProfilesDeleteButtonLeft => LayoutMetrics.R(969);
    public static double ProfilesDeleteButtonTop => LayoutMetrics.R(2510);
    public static double ProfilesDeleteButtonWidth => LayoutMetrics.R(358);
    public static double ProfilesDeleteButtonHeight => LayoutMetrics.R(238);
    public static double ProfilesDeleteHoverLeftOffset => LayoutMetrics.R(20);
    public static double ProfilesDeleteHoverTopOffset => LayoutMetrics.R(19);
    public static double ProfilesDeleteHoverWidth => LayoutMetrics.R(321);
    public static double ProfilesDeleteHoverHeight => LayoutMetrics.R(199);

    // Cancel (2602,2510 421×238) / hover (2626,2530 377×197)
    public static double CancelButtonLeft => LayoutMetrics.R(2602);
    public static double CancelButtonTop => LayoutMetrics.R(2510);
    public static double CancelButtonWidth => LayoutMetrics.R(421);
    public static double CancelButtonHeight => LayoutMetrics.R(238);
    public static double CancelHoverLeftOffset => LayoutMetrics.R(24);
    public static double CancelHoverTopOffset => LayoutMetrics.R(20);
    public static double CancelHoverWidth => LayoutMetrics.R(377);
    public static double CancelHoverHeight => LayoutMetrics.R(197);

    // Save (3023,2510 421×238) / hover (3045,2530 377×197)
    public static double SaveButtonLeft => LayoutMetrics.R(3023);
    public static double SaveButtonTop => LayoutMetrics.R(2510);
    public static double SaveButtonWidth => LayoutMetrics.R(421);
    public static double SaveButtonHeight => LayoutMetrics.R(238);
    public static double SaveHoverLeftOffset => LayoutMetrics.R(22);
    public static double SaveHoverTopOffset => LayoutMetrics.R(20);
    public static double SaveHoverWidth => LayoutMetrics.R(377);
    public static double SaveHoverHeight => LayoutMetrics.R(197);
}
