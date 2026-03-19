using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;
using System.IO;

namespace TransApp;

public class ScreenCaptureService
{
    /// <summary>
    /// 擷取螢幕特定區域並返回位元組陣列。
    /// 目前先使用 GDI+ 進行基礎實作，後續可優化為 Windows.Graphics.Capture。
    /// </summary>
    public byte[] CaptureScreenRegion(int x, int y, int width, int height)
    {
        if (width <= 0 || height <= 0) return Array.Empty<byte>();

        using var bitmap = new Bitmap(width, height);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height));
        }

        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    /// <summary>
    /// 考慮 DPI 縮放的座標轉換。
    /// </summary>
    public (int X, int Y, int W, int H) GetPhysicalCoordinates(double logicalX, double logicalY, double logicalW, double logicalH)
    {
        // 取得主要螢幕的 DPI 縮放比例
        var source = PresentationSource.FromVisual(Application.Current.MainWindow);
        if (source?.CompositionTarget == null)
            return ((int)logicalX, (int)logicalY, (int)logicalW, (int)logicalH);

        double mX = source.CompositionTarget.TransformToDevice.M11;
        double mY = source.CompositionTarget.TransformToDevice.M22;

        return (
            (int)(logicalX * mX),
            (int)(logicalY * mY),
            (int)(logicalW * mX),
            (int)(logicalH * mY)
        );
    }
}
