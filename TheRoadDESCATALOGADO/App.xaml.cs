using System.Windows;
using MahApps.Metro.Controls;
using TheRoad.Services;

namespace TheRoad;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

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
            new MainWindow().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SettingsService.Instance.Save();
        AudioService.Instance.Dispose();
        base.OnExit(e);
    }
}

