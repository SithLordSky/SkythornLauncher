using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SkythornLauncher;

internal static class LauncherAppIcon
{
    public static ImageSource? Load()
    {
        var pngPath = Path.Combine(AppContext.BaseDirectory, "Assets", "STRIcon.png");
        if (!File.Exists(pngPath))
        {
            return null;
        }

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(pngPath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }
}
