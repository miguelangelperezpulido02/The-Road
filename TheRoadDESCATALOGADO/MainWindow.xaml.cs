using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using TheRoad.Converters;
using TheRoad.Services;
using TheRoad.ViewModels;

namespace TheRoad;

public partial class MainWindow : MetroWindow
{
    private readonly CharacterCreatorViewModel _vm = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;

        Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/TheRoad;component/Converters/CommonConverters.xaml", UriKind.Absolute)
        });

        SettingsService.Instance.ApplyWindowState(this);

#if DEBUG
        BtnDebug.Visibility = Visibility.Visible;
#endif
    }

    private void BtnMas_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is string stat)
            _vm.Subir(stat);
    }

    private void BtnMenos_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is string stat)
            _vm.Bajar(stat);
    }

    private void BtnFoto_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Elegir foto del superviviente",
            Filter = "Imágenes (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
        };
        if (dlg.ShowDialog() == true)
            _vm.PhotoPath = dlg.FileName;
    }

    private void BtnQuitarFoto_Click(object sender, RoutedEventArgs e)
    {
        _vm.PhotoPath = null;
    }

    private void BtnAleatorio_Click(object sender, RoutedEventArgs e) => _vm.Aleatorio();

    private void BtnReiniciar_Click(object sender, RoutedEventArgs e) => _vm.Reiniciar();

    private void BtnComenzar_Click(object sender, RoutedEventArgs e)
    {
        var jugador = _vm.BuildPlayer();
        var estado = Logic.GameData.NuevaPartida(jugador);
        var juego = new Views.GameWindow(estado);
        juego.Show();
        Close();
    }

    private void BtnOptions_Click(object sender, RoutedEventArgs e)
    {
        var options = new Views.OptionsWindow();
        options.Owner = this;
        options.ShowDialog();
    }

#if DEBUG
    private void BtnDebug_Click(object sender, RoutedEventArgs e)
    {
        new Views.GameWindow(Logic.GameData.PartidaDebug()).Show();
        Close();
    }
#else
    private void BtnDebug_Click(object sender, RoutedEventArgs e) { }
#endif
}