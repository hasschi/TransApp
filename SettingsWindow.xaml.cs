using System;
using System.Windows;
using System.Windows.Controls;

namespace TransApp;

public partial class SettingsWindow : Window
{
    public Action? SettingsChanged;

    public SettingsWindow()
    {
        InitializeComponent();
        LoadCurrentConfig();
    }

    private void LoadCurrentConfig()
    {
        var config = ConfigService.Current;
        FontSizeSlider.Value = config.FontSize;
        OpacitySlider.Value = config.Opacity;

        foreach (ComboBoxItem item in TargetLangBox.Items)
        {
            if (item.Tag.ToString() == config.ToLanguage)
            {
                TargetLangBox.SelectedItem = item;
                break;
            }
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var config = ConfigService.Current;
        config.FontSize = FontSizeSlider.Value;
        config.Opacity = OpacitySlider.Value;

        if (TargetLangBox.SelectedItem is ComboBoxItem item)
        {
            config.ToLanguage = item.Tag.ToString() ?? "zh-TW";
        }

        ConfigService.Save();
        SettingsChanged?.Invoke();
        this.Close();
    }
}
