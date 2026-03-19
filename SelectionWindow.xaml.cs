using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TransApp;

public partial class SelectionWindow : Window
{
    private Point _startPoint;
    private bool _isSelecting = false;

    public SelectionWindow()
    {
        InitializeComponent();
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

        var x = Canvas.GetLeft(SelectionRect);
        var y = Canvas.GetTop(SelectionRect);
        var width = SelectionRect.Width;
        var height = SelectionRect.Height;

        Console.WriteLine($"Selected Area: X={x}, Y={y}, Width={width}, Height={height}");
        
        this.Close();
    }
}
