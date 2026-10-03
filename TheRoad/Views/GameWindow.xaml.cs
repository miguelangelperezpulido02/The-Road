using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using TheRoad.Logic;
using TheRoad.Models;
using TheRoad.Services;
using TheRoad.ViewModels;

namespace TheRoad.Views;

public partial class GameWindow : MetroWindow
{
    private readonly GameViewModel _vm;

    public GameWindow(GameState state)
    {
        InitializeComponent();

        _vm = new GameViewModel(state, GameData.Locations(), GameData.Routes());
        DataContext = _vm;
        VistaMapa.DataContext = _vm;
        VistaMapa.Refrescar();

        SettingsService.Instance.ApplyWindowState(this);

        // Suscribir a cambios de vista para actualizar mapa
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(GameViewModel.Vista) && _vm.Vista == "Mapa")
                VistaMapa.Refrescar();
        };

#if DEBUG
        // Botones debug están en el header ahora
#endif
    }

    private async void BtnViajar_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.IsTraveling) return;

        // Mostrar overlay de viaje
        TravelOverlay.Visibility = Visibility.Visible;
        TravelStatusText.Text = "En camino...";

        // Esperar al viaje: el overlay cubre exactamente su duración.
        await _vm.ViajarAsync();

        // Actualizar UI post-viaje, ya en destino
        VistaMapa.Refrescar();
        TravelOverlay.Visibility = Visibility.Collapsed;
    }

    private void BtnUsarItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is string item)
        {
            _vm.UsarItem(item);
            VistaMapa.Refrescar();
        }
    }

    private void BtnReparar_Click(object sender, RoutedEventArgs e)
    {
        _vm.RepararVehiculo();
        VistaMapa.Refrescar();
    }

    private void BtnCerrarPopup_Click(object sender, RoutedEventArgs e)
    {
        _vm.CerrarPopup();
    }

    private void BtnOpcionDecision_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is int indice)
            _vm.ElegirOpcion(indice);
    }

    // Barra debug: un solo handler, la acción va en Tag (mismo patrón que BtnUsarItem_Click).
    private void Dbg_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button b || b.Tag is not string tag) return;
        switch (tag)
        {
            case "averia": _vm.DebugForzarAveria(); break;
            case "s100": _vm.DebugSaludVehiculo(100); break;
            case "s50": _vm.DebugSaludVehiculo(50); break;
            case "s1": _vm.DebugSaludVehiculo(1); break;
            case "s0": _vm.DebugSaludVehiculo(0); break;
            case "tirada": _vm.DebugTiradaAveria(); break;
            case "eventoG": _vm.DebugForzarDecision(true); break;
            case "eventoM": _vm.DebugForzarDecision(false); break;
            case "decisionD": _vm.DebugForzarDecision(); break;
            case "gas": _vm.DebugGasolina(20); break;
            case "bidon": _vm.DebugBidon(); break;
            case "hora22": _vm.DebugFijarHora(22); break;
            case "hora8": _vm.DebugFijarHora(8); break;
        }
        VistaMapa.Refrescar();
    }

    private void BtnOptions_Click(object sender, RoutedEventArgs e)
    {
        var options = new OptionsWindow();
        options.Owner = this;
        options.ShowDialog();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implementar guardado
        _vm.AddNotification("Guardado no implementado aún.", NotificationType.Info);
    }
}