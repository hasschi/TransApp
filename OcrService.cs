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
    /// 對軟體點陣圖 (SoftwareBitmap) 進行文字辨識。
    /// </summary>
    public async Task<string> RecognizeTextAsync(SoftwareBitmap bitmap)
    {
        // OCR 引擎要求 SoftwareBitmap 格式必須為 Gray8, Yuy2 或 Bgra8
        SoftwareBitmap? convertedBitmap = null;
        try 
        {
            if (bitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 || 
                bitmap.BitmapAlphaMode != BitmapAlphaMode.Premultiplied)
            {
                convertedBitmap = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            }

            var result = await _ocrEngine.RecognizeAsync(convertedBitmap ?? bitmap);
            return result.Text;
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
