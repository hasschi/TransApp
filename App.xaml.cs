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
    private double _scaleX = 1.0;
    private double _scaleY = 1.0;
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

        // 註冊全域快捷鍵 Alt + Q (選取) 與 Alt + R (編輯)
        try
        {
            HotkeyManager.Current.AddOrReplace("SelectArea", Key.Q, ModifierKeys.Alt, OnSelectArea);
            HotkeyManager.Current.AddOrReplace("EditArea", Key.R, ModifierKeys.Alt, OnEditArea);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"無法註冊快捷鍵: {ex.Message}");
        }
    }

    private void OnSelectArea(object? sender, HotkeyEventArgs e)
    {
        StopMonitoring();

        foreach (Window window in Current.Windows)
        {
            if (window is SelectionWindow) return;
        }

        var selectionWindow = new SelectionWindow();
        selectionWindow.AreaSelected += (rect, sx, sy) =>
        {
            _scaleX = sx;
            _scaleY = sy;
            UpdateAreaAndStartMonitoring(rect);
        };
        selectionWindow.Show();
        selectionWindow.Activate();
    }

    private void OnEditArea(object? sender, HotkeyEventArgs e)
    {
        if (_selectedArea.Width <= 0) 
        {
            MessageBox.Show("請先按 Alt + Q 選取一個區域。");
            return;
        }

        StopMonitoring();

        var transformWindow = new TransformWindow(_selectedArea);
        transformWindow.AreaUpdated += (rect) =>
        {
            UpdateAreaAndStartMonitoring(rect);
        };
        transformWindow.Show();
        transformWindow.Activate();
    }

    private void UpdateAreaAndStartMonitoring(Rect rect)
    {
        _selectedArea = rect;
        _lastText = string.Empty; // 重設快取
        StartMonitoring();
    }

    private void StartMonitoring()
    {
        if (_isMonitoring) return;
        _isMonitoring = true;
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        // 顯示全螢幕 Overlay (它現在負責繪製選取框與文字)
        _overlayWindow!.Show();
        _overlayWindow!.UpdateResult(_selectedArea, string.Empty);

        // 啟動背景監控循環 (約 2Hz)
        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await ProcessTranslationAsync(token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"監控循環錯誤: {ex.Message}");
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
        Dispatcher.Invoke(() => _overlayWindow?.Hide());
    }

    private async Task ProcessTranslationAsync(CancellationToken token)
    {
        try 
        {
            // 1. 座標轉換 (邏輯 -> 物理)
            var physical = _captureService.GetPhysicalCoordinates(
                _selectedArea.X, _selectedArea.Y, _selectedArea.Width, _selectedArea.Height, _scaleX, _scaleY);
            
            System.Diagnostics.Debug.WriteLine($"[Debug] 選取區域: L={_selectedArea.X}, T={_selectedArea.Y}, W={_selectedArea.Width}, H={_selectedArea.Height}, Scale={_scaleX}");

            // 2. 截圖
            var imageBytes = _captureService.CaptureScreenRegion(
                physical.X, physical.Y, physical.W, physical.H);

            if (imageBytes.Length == 0) return;
            token.ThrowIfCancellationRequested();

            // 3. OCR 辨識
            var currentText = await _ocrService.RecognizeFromBytesAsync(imageBytes);
            currentText = currentText.Trim(); // 僅移除頭尾空白，保留內部換行

            if (string.IsNullOrEmpty(currentText)) 
            {
                Dispatcher.Invoke(() => _overlayWindow?.UpdateResult(_selectedArea, string.Empty));
                return;
            }

            if (currentText == _lastText) return;
            _lastText = currentText;
            token.ThrowIfCancellationRequested();

            // 4. 翻譯
            var translated = await _translationService.TranslateAsync(currentText);
            token.ThrowIfCancellationRequested();

            // 5. 更新 UI
            Dispatcher.Invoke(() =>
            {
                _overlayWindow?.UpdateResult(_selectedArea, translated);
            });
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
             System.Diagnostics.Debug.WriteLine($"[Error] 翻譯管道錯誤: {ex.Message}");
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

