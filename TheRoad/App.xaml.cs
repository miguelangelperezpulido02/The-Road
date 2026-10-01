using System.IO;
using System.Windows;
using MahApps.Metro.Controls;
using TheRoad.Services;

namespace TheRoad;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Log global de errores: en vez de cerrarse en silencio, se registra y se muestra
        string crashLog = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TheRoad", "crash.log");

        DispatcherUnhandledException += (_, ex) =>
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(crashLog)!);
                File.AppendAllText(crashLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n{ex.Exception}\n\n");
            }
            catch { }
            ex.Handled = true;
            MessageBox.Show($"{ex.Exception.Message}\n\n{ex.Exception.StackTrace}",
                "The Road — Error", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, ex) =>
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(crashLog)!);
                File.AppendAllText(crashLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] (no-UI)\n{ex.ExceptionObject}\n\n");
            }
            catch { }
        };

        // Initialize services
        _ = SettingsService.Instance;
        _ = AudioService.Instance;

        bool debug = e.Args.Any(a =>
            a.Equals("--debug", StringComparison.OrdinalIgnoreCase) ||
            a.Equals("--hud", StringComparison.OrdinalIgnoreCase) ||
            a.Equals("-d", StringComparison.OrdinalIgnoreCase));

#if DEBUG
        if (!debug && Environment.GetEnvironmentVariable("THE_ROAD_DEBUG") == "1")
            debug = true;
#endif

        if (debug)
            new Views.GameWindow(Logic.GameData.PartidaDebug()).Show();
        else
            new CreadorPJWindow().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SettingsService.Instance.Save();
        AudioService.Instance.Dispose();
        base.OnExit(e);
    }
}

