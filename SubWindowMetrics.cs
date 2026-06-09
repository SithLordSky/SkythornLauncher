using System.Windows;

namespace SkythornLauncher;

public static class SubWindowMetrics
{
    public const double WindowWidth = LayoutMetrics.WindowWidth;
    public const double WindowHeight = LayoutMetrics.WindowHeight;

    // Content inset inside the inner panel (scaled from 1024×768 mockups → 800×600)
    public static Thickness ContentMargin => new(78, 102, 78, 63);
}
