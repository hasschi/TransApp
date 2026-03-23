using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace TransApp;

public partial class TransformWindow : Window
{
    public Action<Rect>? AreaUpdated;
    private Rect _currentRect;

    public TransformWindow(Rect initialArea)
    {
        InitializeComponent();
        
        // 初始視窗覆蓋全虛擬螢幕 (跟 Overlay 一樣)
        this.Left = SystemParameters.VirtualScreenLeft;
        this.Top = SystemParameters.VirtualScreenTop;
        this.Width = SystemParameters.VirtualScreenWidth;
        this.Height = SystemParameters.VirtualScreenHeight;

        _currentRect = initialArea;
        UpdateUI();
    }

    private void UpdateUI()
    {
        // 轉換為 Canvas 局部座標
        double x = _currentRect.X - this.Left;
        double y = _currentRect.Y - this.Top;

        Canvas.SetLeft(TransformBorder, x);
        Canvas.SetTop(TransformBorder, y);
        TransformBorder.Width = _currentRect.Width;
        TransformBorder.Height = _currentRect.Height;

        // 更新提示文字位置
        Canvas.SetLeft(HintPanel, x + (_currentRect.Width / 2) - 100);
        Canvas.SetTop(HintPanel, y + _currentRect.Height + 10);
    }

    private void Handle_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (sender is not Thumb thumb) return;
        string tag = thumb.Tag.ToString() ?? "";

        double newLeft = _currentRect.X;
        double newTop = _currentRect.Y;
        double newWidth = _currentRect.Width;
        double newHeight = _currentRect.Height;

        // 水平拉伸
        if (tag.Contains("W"))
        {
            newLeft += e.HorizontalChange;
            newWidth -= e.HorizontalChange;
        }
        else if (tag.Contains("E"))
        {
            newWidth += e.HorizontalChange;
        }

        // 垂直拉伸
        if (tag.Contains("N"))
        {
            newTop += e.VerticalChange;
            newHeight -= e.VerticalChange;
        }
        else if (tag.Contains("S"))
        {
            newHeight += e.VerticalChange;
        }

        // 最小尺寸限制
        if (newWidth < 20) newWidth = 20;
        if (newHeight < 20) newHeight = 20;

        _currentRect = new Rect(newLeft, newTop, Math.Max(20, newWidth), Math.Max(20, newHeight));
        UpdateUI();
    }

    private void MainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 點擊中間區域可以拖曳整塊移動
        if (e.Source == TransformBorder)
        {
            this.DragMove(); // 這會拖曳整個視窗，不符合需求。改用手動
        }
    }

    // 實作手動拖曳
    private Point _lastMousePos;
    private bool _isDragging = false;

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonDown(e);
        if (e.Source == TransformBorder)
        {
            _isDragging = true;
            _lastMousePos = e.GetPosition(this);
            TransformBorder.CaptureMouse();
        }
    }

    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        base.OnPreviewMouseMove(e);
        if (_isDragging)
        {
            var currentPos = e.GetPosition(this);
            double dx = currentPos.X - _lastMousePos.X;
            double dy = currentPos.Y - _lastMousePos.Y;

            _currentRect = new Rect(_currentRect.X + dx, _currentRect.Y + dy, _currentRect.Width, _currentRect.Height);
            _lastMousePos = currentPos;
            UpdateUI();
        }
    }

    protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonUp(e);
        if (_isDragging)
        {
            _isDragging = false;
            TransformBorder.ReleaseMouseCapture();
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AreaUpdated?.Invoke(_currentRect);
            this.Close();
        }
        else if (e.Key == Key.Escape)
        {
            this.Close();
        }
    }
}
