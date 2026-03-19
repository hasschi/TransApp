using System;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using NHotkey;
using NHotkey.Wpf;
using System.Windows.Input;

namespace TransApp;

public partial class App : Application
{
    private TaskbarIcon? _notifyIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 初始化 System Tray
        _notifyIcon = new TaskbarIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            ToolTipText = "Real-time Screen Translator",
            ContextMenu = (System.Windows.Controls.ContextMenu)FindResource("TrayMenu")
        };

        // 註冊全域快捷鍵 Alt + Q
        try
        {
            HotkeyManager.Current.AddOrReplace("SelectArea", Key.Q, ModifierKeys.Alt, OnSelectArea);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"無法註冊快捷鍵: {ex.Message}");
        }
    }

    private void OnSelectArea(object? sender, HotkeyEventArgs e)
    {
        // 如果已經有一個選取視窗，避免重複開啟
        foreach (Window window in Current.Windows)
        {
            if (window is SelectionWindow) return;
        }

        var selectionWindow = new SelectionWindow();
        selectionWindow.Show();
        selectionWindow.Activate();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Current.Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _notifyIcon?.Dispose();
        base.OnExit(e);
    }
}

