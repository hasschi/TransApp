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
        _isMonitoring = false; // 暫停監控

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

        // 設定 Overlay 位置 (稍微偏移選取區域下方)
        _overlayWindow!.Left = _selectedArea.Left;
        _overlayWindow!.Top = _selectedArea.Bottom + 5;
        _overlayWindow!.Width = _selectedArea.Width;
        _overlayWindow!.Show();

        // 啟動背景監控循環 (約 2Hz)
        Task.Run(async () =>
        {
            while (_isMonitoring)
            {
                try
                {
                    await ProcessTranslationAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"處理錯誤: {ex.Message}");
                }
                await Task.Delay(500);
            }
        });
    }

    private async Task ProcessTranslationAsync()
    {
        // 1. 座標轉換 (邏輯 -> 物理)
        var physical = _captureService.GetPhysicalCoordinates(
            _selectedArea.X, _selectedArea.Y, _selectedArea.Width, _selectedArea.Height);

        // 2. 截圖
        var imageBytes = _captureService.CaptureScreenRegion(
            physical.X, physical.Y, physical.W, physical.H);

        if (imageBytes.Length == 0) return;

        // 3. OCR 辨識
        var currentText = await _ocrService.RecognizeFromBytesAsync(imageBytes);
        currentText = currentText.Replace("\n", " ").Replace("\r", " ").Trim();

        if (string.IsNullOrEmpty(currentText) || currentText == _lastText) return;
        _lastText = currentText;

        // 4. 翻譯
        var translated = await _translationService.TranslateAsync(currentText);

        // 5. 更新 UI
        Dispatcher.Invoke(() =>
        {
            _overlayWindow?.UpdateText(translated);
        });
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        _isMonitoring = false;
        Current.Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _notifyIcon?.Dispose();
        base.OnExit(e);
    }
}

