using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TransApp;

public partial class SelectionWindow : Window
{
    private Point _startPoint;
    private bool _isSelecting = false;
    public Action<Rect, double, double>? AreaSelected;

    public SelectionWindow()
    {
        InitializeComponent();
        
        // 覆蓋所有螢幕
        this.Left = SystemParameters.VirtualScreenLeft;
        this.Top = SystemParameters.VirtualScreenTop;
        this.Width = SystemParameters.VirtualScreenWidth;
        this.Height = SystemParameters.VirtualScreenHeight;
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isSelecting = true;
        _startPoint = e.GetPosition(MainCanvas);
        SelectionRect.Visibility = Visibility.Visible;
        
        Canvas.SetLeft(SelectionRect, _startPoint.X);
        Canvas.SetTop(SelectionRect, _startPoint.Y);
        SelectionRect.Width = 0;
        SelectionRect.Height = 0;
        
        this.CaptureMouse();
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isSelecting) return;
        
        var currentPoint = e.GetPosition(MainCanvas);
        var x = Math.Min(currentPoint.X, _startPoint.X);
        var y = Math.Min(currentPoint.Y, _startPoint.Y);
        var w = Math.Abs(currentPoint.X - _startPoint.X);
        var h = Math.Abs(currentPoint.Y - _startPoint.Y);
        
        Canvas.SetLeft(SelectionRect, x);
        Canvas.SetTop(SelectionRect, y);
        SelectionRect.Width = w;
        SelectionRect.Height = h;
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isSelecting) return;
        _isSelecting = false;
        this.ReleaseMouseCapture();

        var x = Canvas.GetLeft(SelectionRect) + this.Left;
        var y = Canvas.GetTop(SelectionRect) + this.Top;
        var width = SelectionRect.Width;
        var height = SelectionRect.Height;

        if (width > 0 && height > 0)
        {
            // 捕捉當前螢幕的 DPI 縮放
            var source = PresentationSource.FromVisual(this);
            double scaleX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            double scaleY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

            AreaSelected?.Invoke(new Rect(x, y, width, height), scaleX, scaleY);
        }
        
        this.Close();
    }
}
