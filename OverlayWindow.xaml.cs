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

            // 執行智慧定位與響應式佈局
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
        double padding = 6; // 3+3

        // 定義嘗試的位置順序
        // 1. 下方 (寬度同選取區)
        // 2. 上方 (寬度同選取區)
        // 3. 右側 (寬度預設 300)
        
        var strategies = new List<(string Name, double Width, double AvailableHeight, Func<double, double, Point> PosCalculator)>
        {
            ("Below", sourceArea.Width, this.Height - (sourceArea.Bottom - this.Top) - spacing, (w, h) => new Point(sourceArea.X, sourceArea.Bottom + spacing)),
            ("Above", sourceArea.Width, (sourceArea.Top - this.Top) - spacing, (w, h) => new Point(sourceArea.X, sourceArea.Top - h - spacing)),
            ("Right", 300, this.Height - 10, (w, h) => new Point(sourceArea.Right + spacing, sourceArea.Top))
        };

        foreach (var strategy in strategies)
        {
            if (strategy.AvailableHeight < 30) continue; // 空間太小不考慮

            // 模擬在該寬度下的最小所需高度 (縮放至 12px 後)
            var (bestFontSize, requiredHeight) = MeasureMinHeight(text, strategy.Width, baseFontSize, 12, strategy.AvailableHeight - padding);

            if (requiredHeight <= strategy.AvailableHeight - padding)
            {
                // 找到放得下的位置！
                ApplyLayout(strategy.Width, requiredHeight + padding, bestFontSize, strategy.PosCalculator(strategy.Width, requiredHeight + padding));
                return;
            }
        }

        // 如果都放不下，就用最後一個嘗試的位置 (右側) 並讓它截斷
        var last = strategies[2];
        var (f, h) = MeasureMinHeight(text, last.Width, baseFontSize, 12, last.AvailableHeight - padding);
        ApplyLayout(last.Width, last.AvailableHeight, f, last.PosCalculator(last.Width, last.AvailableHeight));
    }

    private (double FontSize, double Height) MeasureMinHeight(string text, double width, double startSize, double minSize, double maxHeight)
    {
        double currentSize = startSize;
        TranslatedText.Width = width - 6; // 減去 Padding
        
        while (currentSize >= minSize)
        {
            TranslatedText.FontSize = currentSize;
            TranslatedText.LineHeight = currentSize * 1.0;
            TranslatedText.UpdateLayout();
            
            double h = TranslatedText.ActualHeight;
            if (h <= maxHeight) return (currentSize, h);
            currentSize -= 1;
        }

        // 如果 12px 還是超出，回傳 12px 時的高度
        TranslatedText.FontSize = minSize;
        TranslatedText.LineHeight = minSize * 1.0;
        TranslatedText.UpdateLayout();
        return (minSize, TranslatedText.ActualHeight);
    }

    private void ApplyLayout(double w, double h, double fontSize, Point screenPos)
    {
        TranslationContainer.Width = w;
        TranslationContainer.Height = h;
        TranslatedText.FontSize = fontSize;
        TranslatedText.LineHeight = fontSize * 1.0;
        TranslatedText.Width = w - 6;

        Canvas.SetLeft(TranslationContainer, screenPos.X - this.Left);
        Canvas.SetTop(TranslationContainer, screenPos.Y - this.Top);
    }
}
