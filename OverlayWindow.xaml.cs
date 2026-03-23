using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;

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

    private Storyboard? _scrollStoryboard;

    public OverlayWindow()
    {
        InitializeComponent();
        
        this.Left = SystemParameters.VirtualScreenLeft;
        this.Top = SystemParameters.VirtualScreenTop;
        this.Width = SystemParameters.VirtualScreenWidth;
        this.Height = SystemParameters.VirtualScreenHeight;

        TranslatedText.RenderTransform = new TranslateTransform();

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
            StopScrolling();
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
                ApplyResponsiveLayout(targetRect);
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    private void ApplyResponsiveLayout(Rect targetRect)
    {
        StopScrolling();
        var config = ConfigService.Current;
        double currentFontSize = config.FontSize;
        
        // 確保初始字體正確
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

        // 階段二：如果縮小到 12px 還是放不下，強制暫時拉大文字框高度
        if (TranslatedText.ActualHeight > TranslationContainer.ActualHeight - 6)
        {
            TranslationContainer.Height = TranslatedText.ActualHeight + 6;
            // 當高度已經完全容納文字時，不需要捲動
            StopScrolling();
        }
        else
        {
            // 如果目前的字體大小已經可以放得下，則檢查是否要啟動捲動 (預防萬一)
            CheckAndStartScrolling(TranslationContainer.Height);
        }
    }

    private void CheckAndStartScrolling(double containerHeight)
    {
        StopScrolling();

        double contentHeight = TranslatedText.ActualHeight;
        double viewableHeight = containerHeight - 6;

        if (contentHeight > viewableHeight)
        {
            double scrollDistance = contentHeight - viewableHeight;
            var animation = new DoubleAnimation
            {
                From = 0,
                To = -scrollDistance - 20,
                Duration = TimeSpan.FromSeconds(Math.Max(3, scrollDistance / 20)),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            _scrollStoryboard = new Storyboard();
            _scrollStoryboard.Children.Add(animation);
            Storyboard.SetTarget(animation, TranslatedText);
            Storyboard.SetTargetProperty(animation, new PropertyPath("RenderTransform.(TranslateTransform.Y)"));
            _scrollStoryboard.Begin();
        }
    }

    private void StopScrolling()
    {
        _scrollStoryboard?.Stop();
        if (TranslatedText.RenderTransform is TranslateTransform tt)
        {
            tt.Y = 0;
        }
    }
}
