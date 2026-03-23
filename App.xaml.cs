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
    private Rect _overlayRect; // 獨立儲存翻譯框的位置與大小
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

        // 註冊全域快捷鍵 Alt + Q (選取來源) 與 Alt + R (調整文字框)
        try
        {
            HotkeyManager.Current.AddOrReplace("SelectArea", Key.Q, ModifierKeys.Alt, OnSelectArea);
            HotkeyManager.Current.AddOrReplace("EditOverlay", Key.R, ModifierKeys.Alt, OnEditOverlay);
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
            _selectedArea = rect;
            
            // 預設翻譯框在選取範圍下方，寬度與選取範圍一致，預設高度 100
            _overlayRect = new Rect(rect.X, rect.Y + rect.Height + 5, rect.Width, 100);
            
            _lastText = string.Empty;
            StartMonitoring();
        };
        selectionWindow.Show();
        selectionWindow.Activate();
    }

    private void OnEditOverlay(object? sender, HotkeyEventArgs e)
    {
        if (_selectedArea.Width <= 0) 
        {
            MessageBox.Show("請先按 Alt + Q 選取一個區域。");
            return;
        }

        StopMonitoring();

        // 調整的是翻譯框 (_overlayRect)
        var transformWindow = new TransformWindow(_overlayRect);
        transformWindow.AreaUpdated += (rect) =>
        {
            _overlayRect = rect;
            StartMonitoring();
        };
        transformWindow.Show();
        transformWindow.Activate();
    }

    private void StartMonitoring()
    {
        if (_isMonitoring) return;
        _isMonitoring = true;
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _overlayWindow!.Show();
        _overlayWindow!.UpdateResult(_selectedArea, _overlayRect, string.Empty);

        // 啟動背景監控循環
        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await ProcessTranslationAsync(token);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"監控循環錯誤: {ex.Message}");
                }
                await Task.Delay(500, token);
            }
        }, token);
    }

    private async Task ProcessTranslationAsync(CancellationToken token)
    {
        try 
        {
            var physical = _captureService.GetPhysicalCoordinates(
                _selectedArea.X, _selectedArea.Y, _selectedArea.Width, _selectedArea.Height, _scaleX, _scaleY);
            
            var imageBytes = _captureService.CaptureScreenRegion(
                physical.X, physical.Y, physical.W, physical.H);

            if (imageBytes.Length == 0) return;
            token.ThrowIfCancellationRequested();

            var currentText = await _ocrService.RecognizeFromBytesAsync(imageBytes);
            currentText = currentText.Trim();

            if (string.IsNullOrEmpty(currentText)) 
            {
                Dispatcher.Invoke(() => _overlayWindow?.UpdateResult(_selectedArea, _overlayRect, string.Empty));
                return;
            }

            if (currentText == _lastText) return;
            _lastText = currentText;
            token.ThrowIfCancellationRequested();

            var translated = await _translationService.TranslateAsync(currentText);
            token.ThrowIfCancellationRequested();

            Dispatcher.Invoke(() =>
            {
                _overlayWindow?.UpdateResult(_selectedArea, _overlayRect, translated);
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

