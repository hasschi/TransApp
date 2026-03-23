using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

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
        
        // 覆蓋所有螢幕
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
        TranslationContainer.Background = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#CC222222"))
            {
                Opacity = config.Opacity
            };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // 設置視窗為滑鼠穿透
        var hwnd = new WindowInteropHelper(this).Handle;
        int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);
    }

    /// <summary>
    /// 更新翻譯結果。
    /// </summary>
    /// <param name="sourceArea">OCR 選取來源區域 (用於顯示藍色提示框)</param>
    /// <param name="targetRect">翻譯文字框位置與大小 (由 Alt+R 調整)</param>
    /// <param name="text">翻譯文字</param>
    public void UpdateResult(Rect sourceArea, Rect targetRect, string text)
    {
        this.Left = SystemParameters.VirtualScreenLeft;
        this.Top = SystemParameters.VirtualScreenTop;

        // 1. 更新 OCR 選取來源提示框 (保持原位)
        SelectionHighlight.Visibility = Visibility.Visible;
        Canvas.SetLeft(SelectionHighlight, sourceArea.X - this.Left);
        Canvas.SetTop(SelectionHighlight, sourceArea.Y - this.Top);
        SelectionHighlight.Width = sourceArea.Width;
        SelectionHighlight.Height = sourceArea.Height;

        // 2. 更新翻譯文字框 (位置由 targetRect 決定)
        if (string.IsNullOrEmpty(text))
        {
            TranslationContainer.Visibility = Visibility.Collapsed;
        }
        else
        {
            TranslationContainer.Visibility = Visibility.Visible;
            TranslatedText.Text = text;

            // 設置文字框位置與大小
            Canvas.SetLeft(TranslationContainer, targetRect.X - this.Left);
            Canvas.SetTop(TranslationContainer, targetRect.Y - this.Top);
            
            // 允許文字框具有固定的寬度與高度（如果使用者有拉伸的話）
            TranslationContainer.Width = targetRect.Width;
            TranslationContainer.Height = targetRect.Height;
        }
    }
}
