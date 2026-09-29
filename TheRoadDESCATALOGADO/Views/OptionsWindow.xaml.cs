using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using TheRoad.Services;

namespace TheRoad.Views;

public partial class OptionsWindow : MetroWindow
{
    private readonly SettingsService _settings = SettingsService.Instance;

    public OptionsWindow()
    {
        InitializeComponent();
        DataContext = _settings;

        Loaded += (_, _) =>
        {
            SfxVolumeSlider.Value = _settings.SfxVolume;
            MusicVolumeSlider.Value = _settings.MusicVolume;
            ShowTooltipsCheck.IsChecked = _settings.ShowTooltips;
            TypewriterCheck.IsChecked = _settings.TypewriterEffect;
            TypewriterSpeedSlider.Value = _settings.TypewriterSpeed;
            RefreshWindowModeButtons();
        };
    }

    private Window? TargetWindow => Owner ?? Application.Current?.MainWindow;

    private void BtnSizeSmall_Click(object sender, RoutedEventArgs e)
    {
        if (TargetWindow != null)
            _settings.ApplyWindowMode(TargetWindow, 1000, 700, fullscreen: false);
        RefreshWindowModeButtons();
    }

    private void BtnSizeLarge_Click(object sender, RoutedEventArgs e)
    {
        if (TargetWindow != null)
            _settings.ApplyWindowMode(TargetWindow, 1280, 720, fullscreen: false);
        RefreshWindowModeButtons();
    }

    private void BtnFullscreen_Click(object sender, RoutedEventArgs e)
    {
        if (TargetWindow != null)
            _settings.ApplyWindowMode(TargetWindow, 0, 0, fullscreen: true);
        RefreshWindowModeButtons();
    }

    private void RefreshWindowModeButtons()
    {
        var active = (Style)FindResource("PrimaryButton");
        var idle = (Style)FindResource("BaseButton");

        BtnSizeSmall.Style = !_settings.Fullscreen && _settings.WindowSettings.Width == 1000 && _settings.WindowSettings.Height == 700 ? active : idle;
        BtnSizeLarge.Style = !_settings.Fullscreen && _settings.WindowSettings.Width == 1280 && _settings.WindowSettings.Height == 720 ? active : idle;
        BtnFullscreen.Style = _settings.Fullscreen ? active : idle;
    }

    private void BtnApply_Click(object sender, RoutedEventArgs e)
    {
        _settings.Save();
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        // Reload from saved
        _settings.Load();
        Close();
    }
}
