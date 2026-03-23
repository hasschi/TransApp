using System;
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
        TranslationContainer.Background = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString("#CC222222"))
            {
                Opacity = config.Opacity
            };
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

            // 1. 先恢復到使用者定義的原始位置與大小
            Canvas.SetLeft(TranslationContainer, targetRect.X - this.Left);
            Canvas.SetTop(TranslationContainer, targetRect.Y - this.Top);
            TranslationContainer.Width = targetRect.Width;
            TranslationContainer.Height = targetRect.Height;

            // 2. 執行響應式佈局邏輯
            this.Dispatcher.BeginInvoke(new Action(() => 
            {
                ApplyResponsiveLayout(sourceArea, targetRect);
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    private void ApplyResponsiveLayout(Rect sourceArea, Rect targetRect)
    {
        var config = ConfigService.Current;
        double currentFontSize = config.FontSize;
        
        TranslatedText.FontSize = currentFontSize;
        TranslatedText.LineHeight = currentFontSize * 1.0;
        this.UpdateLayout();

        // 階段一：嘗試縮小字體 (最小 12px)
        while (TranslatedText.ActualHeight > TranslationContainer.ActualHeight - 6 && currentFontSize > 12)
        {
            currentFontSize -= 1;
            TranslatedText.FontSize = currentFontSize;
            TranslatedText.LineHeight = currentFontSize * 1.0;
            this.UpdateLayout();
        }

        // 階段二：如果縮小到 12px 還是放不下，嘗試暫時拉大文字框
        if (TranslatedText.ActualHeight > TranslationContainer.ActualHeight - 6)
        {
            double desiredHeight = TranslatedText.ActualHeight + 6;
            bool isAbove = targetRect.Bottom <= sourceArea.Top + 10;

            if (isAbove)
            {
                double spaceAbove = targetRect.Bottom - 5;
                double maxAllowedHeight = Math.Min(desiredHeight, spaceAbove);
                
                double deltaHeight = maxAllowedHeight - targetRect.Height;
                Canvas.SetTop(TranslationContainer, (targetRect.Y - this.Top) - deltaHeight);
                TranslationContainer.Height = maxAllowedHeight;
            }
            else
            {
                double screenBottom = this.Height;
                double currentTop = targetRect.Y - this.Top;
                double spaceBelow = screenBottom - currentTop - 5;
                double maxAllowedHeight = Math.Min(desiredHeight, spaceBelow);

                TranslationContainer.Height = maxAllowedHeight;
            }
        }
    }
}
