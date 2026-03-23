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
    /// 更新翻譯結果並繪製提示框。
    /// </summary>
    public void UpdateResult(Rect area, string text)
    {
        // 1. 更新提示框 (Highlight)
        SelectionHighlight.Visibility = Visibility.Visible;
        Canvas.SetLeft(SelectionHighlight, area.X - SystemParameters.VirtualScreenLeft);
        Canvas.SetTop(SelectionHighlight, area.Y - SystemParameters.VirtualScreenTop);
        SelectionHighlight.Width = area.Width;
        SelectionHighlight.Height = area.Height;

        // 2. 更新翻譯文字 (Text)
        if (string.IsNullOrEmpty(text))
        {
            TranslationContainer.Visibility = Visibility.Collapsed;
        }
        else
        {
            TranslationContainer.Visibility = Visibility.Visible;
            TranslatedText.Text = text;

            // 計算文字顯示位置 (選取框下方 5 像素，置中對齊選取框)
            double textLeft = area.X + (area.Width / 2) - (TranslationContainer.ActualWidth / 2);
            double textTop = area.Y + area.Height + 5;
            
            // 修正座標 (減去虛擬螢幕偏移)
            textLeft -= SystemParameters.VirtualScreenLeft;
            textTop -= SystemParameters.VirtualScreenTop;

            Canvas.SetLeft(TranslationContainer, textLeft);
            Canvas.SetTop(TranslationContainer, textTop);
        }
    }
}
