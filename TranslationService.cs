using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Text.Json;
using System.Collections.Generic;

namespace TransApp;

public class TranslationService
{
    private readonly HttpClient _httpClient = new();
    private readonly ConcurrentDictionary<string, string> _cache = new();

    /// <summary>
    /// 使用 Google 翻譯 API (免費 API 端點) 進行翻譯。
    /// </summary>
    public async Task<string> TranslateAsync(string text, string from = "auto", string to = "zh-TW")
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        
        // 檢查快取
        if (_cache.TryGetValue(text, out var cached)) return cached;

        try
        {
            var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={from}&tl={to}&dt=t&q={HttpUtility.UrlEncode(text)}";
            var response = await _httpClient.GetStringAsync(url);
            
            // 解析 JSON
            using var doc = JsonDocument.Parse(response);
            var result = "";
            
            // Google 翻譯的回傳結構中，第一個元素是句子列表
            if (doc.RootElement.GetArrayLength() > 0)
            {
                var sentences = doc.RootElement[0];
                foreach (var sentence in sentences.EnumerateArray())
                {
                    // sentence[0] 是翻譯後的文字，通常會包含原始文本的段落感
                    if (sentence.GetArrayLength() > 0)
                    {
                        result += sentence[0].GetString();
                    }
                }
            }

            if (!string.IsNullOrEmpty(result))
            {
                _cache.TryAdd(text, result);
            }

            return result;
        }
        catch (Exception ex)
        {
            return $"翻譯錯誤: {ex.Message}";
        }
    }
}
