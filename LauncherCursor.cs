using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Win32.SafeHandles;

namespace SkythornLauncher;

internal static class LauncherCursor
{
    private const int CursorSize = 48;
    private const int HotSpotX = 24;
    private const int HotSpotY = 10;

    private static readonly Color TransparentColorKeyLow = Color.FromArgb(0, 0, 0);
    private static readonly Color TransparentColorKeyHigh = Color.FromArgb(24, 24, 24);

    public static Cursor? TryCreate()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "pointer.png");
            if (!File.Exists(path))
            {
                return null;
            }

            using var scaled = LoadScaledCursorBitmap(path, CursorSize);
            return CreateCursor(scaled, HotSpotX, HotSpotY);
        }
        catch
        {
            return null;
        }
    }

    public static void ApplyTo(Window window)
    {
        var cursor = TryCreate();
        if (cursor != null)
        {
            window.Cursor = cursor;
        }
    }

    private static Bitmap LoadScaledCursorBitmap(string path, int size)
    {
        using var source = new Bitmap(path);
        var scaled = new Bitmap(size, size, PixelFormat.Format32bppArgb);

        using var graphics = Graphics.FromImage(scaled);
        using var attributes = new ImageAttributes();

        graphics.Clear(Color.Transparent);
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.CompositingMode = CompositingMode.SourceOver;

        attributes.SetColorKey(TransparentColorKeyLow, TransparentColorKeyHigh);

        var destRect = new Rectangle(0, 0, size, size);
        graphics.DrawImage(
            source,
            destRect,
            0,
            0,
            source.Width,
            source.Height,
            GraphicsUnit.Pixel,
            attributes);

        EnsureAlphaFromBrightness(scaled);
        return scaled;
    }

    private static void EnsureAlphaFromBrightness(Bitmap bitmap)
    {
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var data = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        try
        {
            for (var y = 0; y < bitmap.Height; y++)
            {
                for (var x = 0; x < bitmap.Width; x++)
                {
                    var offset = y * data.Stride + (x * 4);
                    var blue = Marshal.ReadByte(data.Scan0, offset);
                    var green = Marshal.ReadByte(data.Scan0, offset + 1);
                    var red = Marshal.ReadByte(data.Scan0, offset + 2);
                    var alpha = Marshal.ReadByte(data.Scan0, offset + 3);

                    if (alpha == 0)
                    {
                        continue;
                    }

                    if (IsTransparentBackground(red, green, blue))
                    {
                        Marshal.WriteByte(data.Scan0, offset + 3, 0);
                    }
                }
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    private static bool IsTransparentBackground(byte red, byte green, byte blue) =>
        red <= TransparentColorKeyHigh.R
        && green <= TransparentColorKeyHigh.G
        && blue <= TransparentColorKeyHigh.B;

    private static Cursor CreateCursor(Bitmap bitmap, int hotX, int hotY)
    {
        var iconInfo = new IconInfo
        {
            fIcon = false,
            xHotspot = hotX,
            yHotspot = hotY
        };

        using var colorBitmap = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(colorBitmap))
        {
            graphics.DrawImage(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
        }

        using var maskBitmap = CreateMaskBitmap(bitmap);

        iconInfo.hbmColor = colorBitmap.GetHbitmap(Color.FromArgb(0));
        iconInfo.hbmMask = maskBitmap.GetHbitmap();

        try
        {
            var cursorHandle = CreateIconIndirect(ref iconInfo);
            if (cursorHandle == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return CursorInteropHelper.Create(new SafeFileHandle(cursorHandle, ownsHandle: true));
        }
        finally
        {
            DeleteObject(iconInfo.hbmColor);
            DeleteObject(iconInfo.hbmMask);
        }
    }

    private static Bitmap CreateMaskBitmap(Bitmap bitmap)
    {
        var mask = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

        var sourceData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var maskData = mask.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            for (var y = 0; y < bitmap.Height; y++)
            {
                for (var x = 0; x < bitmap.Width; x++)
                {
                    var sourceOffset = y * sourceData.Stride + (x * 4);
                    var maskOffset = y * maskData.Stride + (x * 4);
                    var alpha = Marshal.ReadByte(sourceData.Scan0, sourceOffset + 3);
                    var value = alpha < 128 ? (byte)255 : (byte)0;

                    Marshal.WriteByte(maskData.Scan0, maskOffset, value);
                    Marshal.WriteByte(maskData.Scan0, maskOffset + 1, value);
                    Marshal.WriteByte(maskData.Scan0, maskOffset + 2, value);
                    Marshal.WriteByte(maskData.Scan0, maskOffset + 3, 255);
                }
            }
        }
        finally
        {
            bitmap.UnlockBits(sourceData);
            mask.UnlockBits(maskData);
        }

        return mask;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IconInfo
    {
        public bool fIcon;
        public int xHotspot;
        public int yHotspot;
        public IntPtr hbmMask;
        public IntPtr hbmColor;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr CreateIconIndirect(ref IconInfo icon);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);
}
