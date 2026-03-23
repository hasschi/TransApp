using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;

namespace TransApp;

public class ScreenCaptureService
{
    private readonly IDirect3DDevice _device;

    public ScreenCaptureService()
    {
        _device = Direct3D11Helper.CreateDevice();
    }

    /// <summary>
    /// 擷取螢幕特定區域並返回位元組陣列。
    /// 目前維持 GDI+ 進行基礎實作，確保穩定性。
    /// 後續可透過 _device 擴充為全硬體加速截圖。
    /// </summary>
    public byte[] CaptureScreenRegion(int x, int y, int width, int height)
    {
        if (width <= 0 || height <= 0) return Array.Empty<byte>();

        try
        {
            using var bitmap = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height));
            }

            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Error] GDI+ 截圖失敗: {ex.Message}");
            return Array.Empty<byte>();
        }
    }

    /// <summary>
    /// 考慮 DPI 縮放的座標轉換。
    /// </summary>
    public (int X, int Y, int W, int H) GetPhysicalCoordinates(double logicalX, double logicalY, double logicalW, double logicalH, double scaleX, double scaleY)
    {
        return (
            (int)(logicalX * scaleX),
            (int)(logicalY * scaleY),
            (int)(logicalW * scaleX),
            (int)(logicalH * scaleY)
        );
    }
}
