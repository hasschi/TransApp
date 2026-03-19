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
        // 優先嘗試使用繁體中文辨識引擎，否則使用系統預設
        var lang = new Windows.Globalization.Language("zh-Hant-TW");
        if (OcrEngine.IsLanguageSupported(lang))
        {
            _ocrEngine = OcrEngine.TryCreateFromLanguage(lang);
        }
        else
        {
            _ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
        }

        if (_ocrEngine == null)
        {
            throw new Exception("無法初始化 OCR 引擎。");
        }
    }

    /// <summary>
    /// 對軟體點陣圖 (SoftwareBitmap) 進行文字辨識。
    /// </summary>
    public async Task<string> RecognizeTextAsync(SoftwareBitmap bitmap)
    {
        var result = await _ocrEngine.RecognizeAsync(bitmap);
        return result.Text;
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
        
        // OCR 引擎要求 SoftwareBitmap 格式必須為 Gray8, Yuy2 或 Bgra8
        using var convertedBitmap = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
        
        return await RecognizeTextAsync(convertedBitmap);
    }
}
