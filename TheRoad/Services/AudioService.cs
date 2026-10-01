using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Media;
using NAudio.Wave;

namespace TheRoad.Services
{
    public sealed class AudioService : IDisposable
    {
        private static readonly Lazy<AudioService> _instance = new(() => new AudioService());
        public static AudioService Instance => _instance.Value;

        private readonly ConcurrentDictionary<string, byte[]> _soundCache = new();
        private readonly object _playLock = new();
        private WaveOutEvent? _musicPlayer;
        private AudioFileReader? _musicReader;
        private float _sfxVolume = 0.7f;
        private float _musicVolume = 0.4f;
        private bool _disposed;

        private AudioService()
        {
            PreloadSounds();
        }

        private void PreloadSounds()
        {
            var soundsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sounds");
            if (!Directory.Exists(soundsPath)) return;

            var extensions = new[] { ".wav", ".mp3", ".ogg" };
            foreach (var file in Directory.GetFiles(soundsPath))
            {
                if (Array.Exists(extensions, ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    try
                    {
                        _soundCache[name] = File.ReadAllBytes(file);
                    }
                    catch { }
                }
            }
        }

        public void PlaySfx(string name)
        {
            if (!_soundCache.TryGetValue(name, out var data)) return;

            Task.Run(() =>
            {
                try
                {
                    using var ms = new MemoryStream(data);
                    using var reader = new WaveFileReader(ms);
                    using var player = new WaveOutEvent();
                    player.Volume = _sfxVolume;
                    player.Init(reader);
                    player.Play();
                    while (player.PlaybackState == PlaybackState.Playing)
                        System.Threading.Thread.Sleep(50);
                }
                catch { }
            });
        }

        public void PlayMusic(string name, bool loop = true)
        {
            if (!_soundCache.TryGetValue(name, out var data)) return;

            StopMusic();

            try
            {
                // Save to temp file for AudioFileReader which prefers file paths
                var tempPath = Path.Combine(Path.GetTempPath(), $"theroad_{name}.tmp");
                File.WriteAllBytes(tempPath, data);
                
                _musicReader = new AudioFileReader(tempPath);
                _musicPlayer = new WaveOutEvent();
                _musicPlayer.Volume = _musicVolume;
                _musicPlayer.Init(_musicReader);
                _musicPlayer.PlaybackStopped += (_, _) =>
                {
                    if (loop && _musicReader != null)
                    {
                        _musicReader.Position = 0;
                        _musicPlayer.Play();
                    }
                    else
                    {
                        CleanupMusic();
                        try { File.Delete(tempPath); } catch { }
                    }
                };
                _musicPlayer.Play();
            }
            catch
            {
                CleanupMusic();
            }
        }

        public void StopMusic()
        {
            lock (_playLock)
            {
                _musicPlayer?.Stop();
                CleanupMusic();
            }
        }

        private void CleanupMusic()
        {
            _musicPlayer?.Dispose();
            _musicPlayer = null;
            _musicReader?.Dispose();
            _musicReader = null;
        }

        public void SetSfxVolume(float volume)
        {
            _sfxVolume = Math.Clamp(volume, 0f, 1f);
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Math.Clamp(volume, 0f, 1f);
            if (_musicPlayer != null)
                _musicPlayer.Volume = _musicVolume;
        }

        public float SfxVolume => _sfxVolume;
        public float MusicVolume => _musicVolume;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            StopMusic();
        }
    }
}