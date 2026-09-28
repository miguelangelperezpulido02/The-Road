using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using TheRoad.ViewModels;

namespace TheRoad;

// Solo cablea botones con el ViewModel. Sin reglas de juego aquí.
public partial class MainWindow : Window
{
    private readonly CharacterCreatorViewModel _vm = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;
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
        MessageBox.Show($"{jugador.Resumen()}\n\nEl viaje Washington D.C. → Los Ángeles comenzará aquí en la siguiente fase.",
            "Superviviente creado", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
