using System;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;

namespace TransApp;

public class OcrService
{
    private readonly OcrEngine _ocrEngine;

    public OcrService()
    {
        // 優先使用使用者偏好的語言（通常包含日文、英文等已安裝的語言包）
        _ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();

        // 如果失敗，再嘗試繁體中文
        if (_ocrEngine == null)
        {
            var lang = new Windows.Globalization.Language("zh-Hant-TW");
            if (OcrEngine.IsLanguageSupported(lang))
            {
                _ocrEngine = OcrEngine.TryCreateFromLanguage(lang);
            }
        }

        if (_ocrEngine == null)
        {
            throw new Exception("無法初始化 OCR 引擎，請確保 Windows 已安裝相關語言包。");
        }
    }

    /// <summary>
    /// 對軟體點陣圖 (SoftwareBitmap) 進行文字辨識，並優化段落合併。
    /// </summary>
    public async Task<string> RecognizeTextAsync(SoftwareBitmap bitmap)
    {
        SoftwareBitmap? convertedBitmap = null;
        try 
        {
            if (bitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 || 
                bitmap.BitmapAlphaMode != BitmapAlphaMode.Premultiplied)
            {
                convertedBitmap = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            }

            var result = await _ocrEngine.RecognizeAsync(convertedBitmap ?? bitmap);
            
            if (result.Lines.Count == 0) return string.Empty;

            // 進階段落合併邏輯
            var builder = new System.Text.StringBuilder();
            Windows.Foundation.Rect? lastRect = null;

            foreach (var line in result.Lines)
            {
                // 計算整行的 BoundingRect (OcrLine 本身沒有 Rect 屬性)
                double minX = double.MaxValue, minY = double.MaxValue;
                double maxX = double.MinValue, maxY = double.MinValue;

                foreach (var word in line.Words)
                {
                    var r = word.BoundingRect;
                    minX = Math.Min(minX, r.X);
                    minY = Math.Min(minY, r.Y);
                    maxX = Math.Max(maxX, r.X + r.Width);
                    maxY = Math.Max(maxY, r.Y + r.Height);
                }

                var currentRect = new Windows.Foundation.Rect(minX, minY, maxX - minX, maxY - minY);

                if (lastRect != null)
                {
                    // 判斷兩行之間的距離
                    // 如果當前行的頂部與上一行底部的距離，超過上一行高度的一半，則視為強制換行
                    double gap = currentRect.Y - (lastRect.Value.Y + lastRect.Value.Height);
                    double lineHeight = lastRect.Value.Height;

                    if (gap > lineHeight * 0.5)
                    {
                        builder.Append("\n"); // 縮小段落間距，僅保留單個換行
                    }
                    else if (gap > -lineHeight * 0.5)
                    {
                        builder.Append(" "); // 視為同一段落，僅加空白
                    }
                }

                builder.Append(line.Text);
                lastRect = currentRect;
            }

            return builder.ToString();
        }
        finally
        {
            convertedBitmap?.Dispose();
        }
    }

    /// <summary>
    /// 將位元組陣列轉換為 SoftwareBitmap 後進行辨識。
    /// </summary>
    public async Task<string> RecognizeFromBytesAsync(byte[] imageBytes)
    {
        using var stream = new InMemoryRandomAccessStream();
        await stream.WriteAsync(imageBytes.AsBuffer());
        stream.Seek(0);

        var decoder = await BitmapDecoder.CreateAsync(stream);
        using var bitmap = await decoder.GetSoftwareBitmapAsync();
        
        return await RecognizeTextAsync(bitmap);
    }
}
