using System.Windows;
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

        // Bind slider value display
        UiScaleSlider.ValueChanged += (_, _) => UiScaleValue.Text = $"{_settings.UiScale:P0}";

        Loaded += (_, _) =>
        {
            SfxVolumeSlider.Value = _settings.SfxVolume;
            MusicVolumeSlider.Value = _settings.MusicVolume;
            UiScaleSlider.Value = _settings.UiScale;
            ShowTooltipsCheck.IsChecked = _settings.ShowTooltips;
            TypewriterCheck.IsChecked = _settings.TypewriterEffect;
            TypewriterSpeedSlider.Value = _settings.TypewriterSpeed;
        };
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