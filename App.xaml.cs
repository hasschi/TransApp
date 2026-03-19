using System;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using NHotkey;
using NHotkey.Wpf;
using System.Windows.Input;

namespace TransApp;

public partial class App : Application
{
    private TaskbarIcon? _notifyIcon;
    private OverlayWindow? _overlayWindow;
    private readonly OcrService _ocrService = new();
    private readonly TranslationService _translationService = new();
    private readonly ScreenCaptureService _captureService = new();
    
    private Rect _selectedArea;
    private CancellationTokenSource? _cts;
    private bool _isMonitoring = false;
    private string _lastText = string.Empty;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 初始化 Overlay 視窗
        _overlayWindow = new OverlayWindow();

        // 初始化 System Tray
        _notifyIcon = new TaskbarIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            ToolTipText = "Real-time Screen Translator",
            ContextMenu = (System.Windows.Controls.ContextMenu)FindResource("TrayMenu")
        };

        // 註冊全域快捷鍵 Alt + Q
        try
        {
            HotkeyManager.Current.AddOrReplace("SelectArea", Key.Q, ModifierKeys.Alt, OnSelectArea);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"無法註冊快捷鍵: {ex.Message}");
        }
    }

    private void OnSelectArea(object? sender, HotkeyEventArgs e)
    {
        StopMonitoring(); // 先停止舊的監控

        foreach (Window window in Current.Windows)
        {
            if (window is SelectionWindow) return;
        }

        var selectionWindow = new SelectionWindow();
        selectionWindow.AreaSelected += (rect) =>
        {
            _selectedArea = rect;
            StartMonitoring();
        };
        selectionWindow.Show();
        selectionWindow.Activate();
    }

    private void StartMonitoring()
    {
        if (_isMonitoring) return;
        _isMonitoring = true;
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        // 設定 Overlay 位置 (稍微偏移選取區域下方)
        _overlayWindow!.Left = _selectedArea.Left;
        _overlayWindow!.Top = _selectedArea.Bottom + 5;
        _overlayWindow!.Width = _selectedArea.Width;
        _overlayWindow!.Show();

        // 啟動背景監控循環 (約 2Hz)
        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await ProcessTranslationAsync();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // 在控制台輸出錯誤
                    Console.WriteLine($"監控循環錯誤: {ex.Message}");
                }
                await Task.Delay(500, token);
            }
        }, token);
    }

    private void StopMonitoring()
    {
        _isMonitoring = false;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _overlayWindow?.Hide();
    }

    private async Task ProcessTranslationAsync()
    {
        try 
        {
            // 1. 座標轉換 (邏輯 -> 物理)
            var physical = _captureService.GetPhysicalCoordinates(
                _selectedArea.X, _selectedArea.Y, _selectedArea.Width, _selectedArea.Height);
            
            Console.WriteLine($"[Debug] 選取區域: L={_selectedArea.X}, T={_selectedArea.Y}, W={_selectedArea.Width}, H={_selectedArea.Height}");
            Console.WriteLine($"[Debug] 物理座標: X={physical.X}, Y={physical.Y}, W={physical.W}, H={physical.H}");

            // 2. 截圖
            var imageBytes = _captureService.CaptureScreenRegion(
                physical.X, physical.Y, physical.W, physical.H);

            if (imageBytes.Length == 0) 
            {
                Console.WriteLine("[Debug] 截圖失敗: imageBytes 長度為 0");
                return;
            }
            Console.WriteLine($"[Debug] 截圖成功: {imageBytes.Length} bytes");

            // 3. OCR 辨識
            var currentText = await _ocrService.RecognizeFromBytesAsync(imageBytes);
            currentText = currentText.Replace("\n", " ").Replace("\r", " ").Trim();

            if (string.IsNullOrEmpty(currentText)) 
            {
                Console.WriteLine("[Debug] OCR 未辨識到文字");
                Dispatcher.Invoke(() => _overlayWindow?.UpdateText(string.Empty));
                return;
            }
            Console.WriteLine($"[Debug] OCR 辨識結果: {currentText}");

            if (currentText == _lastText) return;
            _lastText = currentText;

            // 4. 翻譯
            Console.WriteLine($"[Debug] 正在請求翻譯: {currentText}");
            var translated = await _translationService.TranslateAsync(currentText);
            Console.WriteLine($"[Debug] 翻譯結果: {translated}");

            // 5. 更新 UI
            Dispatcher.Invoke(() =>
            {
                _overlayWindow?.UpdateText(translated);
            });
        }
        catch (Exception ex)
        {
             Console.WriteLine($"[Error] 翻譯管道錯誤: {ex.Message}");
             Console.WriteLine($"[Error] StackTrace: {ex.StackTrace}");
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        StopMonitoring();
        Current.Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _notifyIcon?.Dispose();
        base.OnExit(e);
    }
}

