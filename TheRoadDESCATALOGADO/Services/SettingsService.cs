using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using Newtonsoft.Json;

namespace TheRoad.Services
{
    public sealed class SettingsService : INotifyPropertyChanged
    {
        private static readonly Lazy<SettingsService> _instance = new(() => new SettingsService());
        public static SettingsService Instance => _instance.Value;

        private readonly string _settingsPath;
        private WindowSettings _windowSettings = new();
        private float _sfxVolume = 0.7f;
        private float _musicVolume = 0.4f;
        private bool _fullscreen = false;
        private double _uiScale = 1.0;
        private bool _showTooltips = true;
        private bool _typewriterEffect = true;
        private int _typewriterSpeed = 30;

        private SettingsService()
        {
            _settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TheRoad",
                "settings.json");

            Load();
        }

        public WindowSettings WindowSettings
        {
            get => _windowSettings;
            set { _windowSettings = value; OnPropertyChanged(); }
        }

        public float SfxVolume
        {
            get => _sfxVolume;
            set { _sfxVolume = Math.Clamp(value, 0f, 1f); OnPropertyChanged(); AudioService.Instance.SetSfxVolume(_sfxVolume); }
        }

        public float MusicVolume
        {
            get => _musicVolume;
            set { _musicVolume = Math.Clamp(value, 0f, 1f); OnPropertyChanged(); AudioService.Instance.SetMusicVolume(_musicVolume); }
        }

        public bool Fullscreen
        {
            get => _fullscreen;
            set { _fullscreen = value; OnPropertyChanged(); }
        }

        public double UiScale
        {
            get => _uiScale;
            set { _uiScale = Math.Clamp(value, 0.75, 1.5); OnPropertyChanged(); }
        }

        public bool ShowTooltips
        {
            get => _showTooltips;
            set { _showTooltips = value; OnPropertyChanged(); }
        }

        public bool TypewriterEffect
        {
            get => _typewriterEffect;
            set { _typewriterEffect = value; OnPropertyChanged(); }
        }

        public int TypewriterSpeed
        {
            get => _typewriterSpeed;
            set { _typewriterSpeed = Math.Clamp(value, 10, 100); OnPropertyChanged(); }
        }

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(_settingsPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var data = new
                {
                    WindowSettings = _windowSettings,
                    SfxVolume = _sfxVolume,
                    MusicVolume = _musicVolume,
                    Fullscreen = _fullscreen,
                    UiScale = _uiScale,
                    ShowTooltips = _showTooltips,
                    TypewriterEffect = _typewriterEffect,
                    TypewriterSpeed = _typewriterSpeed
                };

                File.WriteAllText(_settingsPath, JsonConvert.SerializeObject(data, Formatting.Indented));
            }
            catch { }
        }

        public void Load()
        {
            try
            {
                if (!File.Exists(_settingsPath)) return;

                var json = File.ReadAllText(_settingsPath);
                var data = JsonConvert.DeserializeAnonymousType(json, new
                {
                    WindowSettings = new WindowSettings(),
                    SfxVolume = 0.7f,
                    MusicVolume = 0.4f,
                    Fullscreen = false,
                    UiScale = 1.0,
                    ShowTooltips = true,
                    TypewriterEffect = true,
                    TypewriterSpeed = 30
                });

                if (data != null)
                {
                    _windowSettings = data.WindowSettings ?? new WindowSettings();
                    _sfxVolume = data.SfxVolume;
                    _musicVolume = data.MusicVolume;
                    _fullscreen = data.Fullscreen;
                    _uiScale = data.UiScale;
                    _showTooltips = data.ShowTooltips;
                    _typewriterEffect = data.TypewriterEffect;
                    _typewriterSpeed = data.TypewriterSpeed;
                }

                AudioService.Instance.SetSfxVolume(_sfxVolume);
                AudioService.Instance.SetMusicVolume(_musicVolume);
            }
            catch { }
        }

        public void ApplyWindowState(Window window)
        {
            if (_windowSettings.Width > 0 && _windowSettings.Height > 0)
            {
                window.Width = _windowSettings.Width;
                window.Height = _windowSettings.Height;
            }
            if (_windowSettings.Left >= 0 && _windowSettings.Top >= 0)
            {
                window.Left = _windowSettings.Left;
                window.Top = _windowSettings.Top;
            }
            if (_windowSettings.IsMaximized)
                window.WindowState = WindowState.Maximized;

            window.Loaded += (_, _) =>
            {
                window.StateChanged += (_, _) => CaptureWindowState(window);
                window.LocationChanged += (_, _) => CaptureWindowState(window);
                window.SizeChanged += (_, _) => CaptureWindowState(window);
            };
        }

        private void CaptureWindowState(Window window)
        {
            if (window.WindowState == WindowState.Normal)
            {
                _windowSettings.Width = window.Width;
                _windowSettings.Height = window.Height;
                _windowSettings.Left = window.Left;
                _windowSettings.Top = window.Top;
                _windowSettings.IsMaximized = false;
            }
            else if (window.WindowState == WindowState.Maximized)
            {
                _windowSettings.IsMaximized = true;
            }
            Save();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class WindowSettings
    {
        public double Width { get; set; } = 1000;
        public double Height { get; set; } = 660;
        public double Left { get; set; } = -1;
        public double Top { get; set; } = -1;
        public bool IsMaximized { get; set; } = false;
    }
}