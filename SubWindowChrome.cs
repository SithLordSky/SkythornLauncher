using System.Windows;
using System.Windows.Input;

namespace SkythornLauncher;

internal static class SubWindowChrome
{
    public static void EnableDragMove(Window window)
    {
        window.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                window.DragMove();
            }
        };
    }
}
