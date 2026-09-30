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
        // Mostrar overlay de viaje
        TravelOverlay.Visibility = Visibility.Visible;
        TravelStatusText.Text = "Preparando vehículo...";

        // El VM maneja la animación internamente
        _vm.Viajar();

        // Actualizar UI post-viaje
        VistaMapa.Refrescar();

        // Ocultar overlay tras un momento
        await Task.Delay(500);
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