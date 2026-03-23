using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace TransApp;

public partial class OverlayWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    public OverlayWindow()
    {
        InitializeComponent();
        this.Left = SystemParameters.VirtualScreenLeft;
        this.Top = SystemParameters.VirtualScreenTop;
        this.Width = SystemParameters.VirtualScreenWidth;
        this.Height = SystemParameters.VirtualScreenHeight;
        ApplySettings();
    }

    public void ApplySettings()
    {
        var config = ConfigService.Current;
        TranslatedText.FontSize = config.FontSize;
        TranslatedText.LineHeight = config.FontSize * 1.0;
        TranslationContainer.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC222222")) { Opacity = config.Opacity };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);
    }

    public void UpdateResult(Rect sourceArea, Rect targetRect, string text)
    {
        this.Left = SystemParameters.VirtualScreenLeft;
        this.Top = SystemParameters.VirtualScreenTop;

        SelectionHighlight.Visibility = Visibility.Visible;
        Canvas.SetLeft(SelectionHighlight, sourceArea.X - this.Left);
        Canvas.SetTop(SelectionHighlight, sourceArea.Y - this.Top);
        SelectionHighlight.Width = sourceArea.Width;
        SelectionHighlight.Height = sourceArea.Height;

        if (string.IsNullOrEmpty(text))
        {
            TranslationContainer.Visibility = Visibility.Collapsed;
        }
        else
        {
            TranslationContainer.Visibility = Visibility.Visible;
            TranslatedText.Text = text;

            this.Dispatcher.BeginInvoke(new Action(() => 
            {
                SmartPositionAndScale(sourceArea, targetRect, text);
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    private void SmartPositionAndScale(Rect sourceArea, Rect targetRect, string text)
    {
        var config = ConfigService.Current;
        double baseFontSize = config.FontSize;
        double spacing = 5;
        double padding = 6;

        // 計算絕對螢幕邊界
        double screenTop = SystemParameters.VirtualScreenTop;
        double screenBottom = screenTop + SystemParameters.VirtualScreenHeight;
        double screenRight = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth;

        var strategies = new List<(string Name, double Width, double AvailableHeight, Func<double, double, Point> PosCalculator)>
        {
            // 1. 下方
            ("Below", sourceArea.Width, screenBottom - sourceArea.Bottom - spacing - 10, 
                (w, h) => new Point(sourceArea.X, sourceArea.Bottom + spacing)),
            
            // 2. 上方
            ("Above", sourceArea.Width, sourceArea.Top - screenTop - spacing - 10, 
                (w, h) => new Point(sourceArea.X, sourceArea.Top - h - spacing)),
            
            // 3. 右側
            ("Right", 300, screenBottom - screenTop - 20, 
                (w, h) => new Point(Math.Min(sourceArea.Right + spacing, screenRight - 310), sourceArea.Y))
        };

        foreach (var strategy in strategies)
        {
            if (strategy.AvailableHeight < 40) continue;

            var (bestFontSize, requiredHeight) = MeasureMinHeight(text, strategy.Width, baseFontSize, 12, strategy.AvailableHeight - padding);

            if (requiredHeight <= strategy.AvailableHeight - padding)
            {
                ApplyLayout(strategy.Width, requiredHeight + padding, bestFontSize, strategy.PosCalculator(strategy.Width, requiredHeight + padding));
                return;
            }
        }

        // 保底：右側
        var last = strategies[2];
        var (f, h) = MeasureMinHeight(text, last.Width, baseFontSize, 12, last.AvailableHeight - padding);
        ApplyLayout(last.Width, last.AvailableHeight, f, last.PosCalculator(last.Width, last.AvailableHeight));
    }

    private (double FontSize, double Height) MeasureMinHeight(string text, double width, double startSize, double minSize, double maxHeight)
    {
        double currentSize = startSize;
        TranslatedText.Width = width - 6;
        
        while (currentSize >= minSize)
        {
            TranslatedText.FontSize = currentSize;
            TranslatedText.LineHeight = currentSize * 1.0;
            TranslatedText.UpdateLayout();
            
            double h = TranslatedText.ActualHeight;
            if (h <= maxHeight) return (currentSize, h);
            currentSize -= 1;
        }

        TranslatedText.FontSize = minSize;
        TranslatedText.LineHeight = minSize * 1.0;
        TranslatedText.UpdateLayout();
        return (minSize, TranslatedText.ActualHeight);
    }

    private void ApplyLayout(double w, double h, double fontSize, Point screenPos)
    {
        // 最終安全檢查：確保不會超出螢幕邊界
        double screenTop = SystemParameters.VirtualScreenTop;
        double screenBottom = screenTop + SystemParameters.VirtualScreenHeight;
        
        double finalTop = screenPos.Y;
        if (finalTop + h > screenBottom) finalTop = screenBottom - h - 5;
        if (finalTop < screenTop) finalTop = screenTop + 5;

        TranslationContainer.Width = w;
        TranslationContainer.Height = h;
        TranslatedText.FontSize = fontSize;
        TranslatedText.LineHeight = fontSize * 1.0;
        TranslatedText.Width = w - 6;

        Canvas.SetLeft(TranslationContainer, screenPos.X - this.Left);
        Canvas.SetTop(TranslationContainer, finalTop - this.Top);
    }
}
