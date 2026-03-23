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

        // 初始化變換，用於滾動
        TranslatedText.RenderTransform = new TranslateTransform();

        ApplySettings();
    }

    public void ApplySettings()
    {
        var config = ConfigService.Current;
        TranslatedText.FontSize = config.FontSize;
        TranslatedText.LineHeight = config.FontSize * 1.0; // 將行高壓縮到 1.0
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

            Canvas.SetLeft(TranslationContainer, targetRect.X - this.Left);
            Canvas.SetTop(TranslationContainer, targetRect.Y - this.Top);
            TranslationContainer.Width = targetRect.Width;
            TranslationContainer.Height = targetRect.Height;

            // 佈局更新後檢查是否需要滾動
            this.Dispatcher.BeginInvoke(new Action(() => 
            {
                CheckAndStartScrolling(targetRect.Height);
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    private void CheckAndStartScrolling(double containerHeight)
    {
        StopScrolling();

        // 取得內容的真實高度 (減去 Padding)
        double contentHeight = TranslatedText.ActualHeight;
        double viewableHeight = containerHeight - 16; // 16 是 Padding (8+8)

        if (contentHeight > viewableHeight)
        {
            // 需要滾動：內容比容器高
            double scrollDistance = contentHeight - viewableHeight;
            
            // 建立動畫：從 0 到 -scrollDistance，然後再回彈或重新循環
            // 這裡採用緩慢向下的效果
            var animation = new DoubleAnimation
            {
                From = 0,
                To = -scrollDistance - 20, // 多滾動一點點確保看到底
                Duration = TimeSpan.FromSeconds(Math.Max(3, scrollDistance / 20)), // 根據距離計算速度
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
