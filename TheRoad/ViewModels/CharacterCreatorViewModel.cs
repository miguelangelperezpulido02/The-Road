using System.ComponentModel;
using TheRoad.Logic;
using TheRoad.Models;

namespace TheRoad.ViewModels;

// Estado de la pantalla de creación. Sin referencias a WPF.
// MainWindow solo hace binding y llama a Subir/Bajar/ElegirFoto.
public class CharacterCreatorViewModel : INotifyPropertyChanged
{
    private readonly Player _player = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Nombre
    {
        get => _player.Name;
        set { _player.Name = value.TrimStart(); Avisar(); }
    }

    public string? PhotoPath
    {
        get => _player.PhotoPath;
        set { _player.PhotoPath = value; Avisar(); }
    }

    public bool TieneFoto => !string.IsNullOrWhiteSpace(_player.PhotoPath);

    public int Fuerza { get => _player.Fuerza; set { CharacterCreator.SetStat(_player, "Fuerza", value); Avisar(); } }
    public int Destreza { get => _player.Destreza; set { CharacterCreator.SetStat(_player, "Destreza", value); Avisar(); } }
    public int Resistencia { get => _player.Resistencia; set { CharacterCreator.SetStat(_player, "Resistencia", value); Avisar(); } }
    public int Inteligencia { get => _player.Inteligencia; set { CharacterCreator.SetStat(_player, "Inteligencia", value); Avisar(); } }
    public int Percepcion { get => _player.Percepcion; set { CharacterCreator.SetStat(_player, "Percepcion", value); Avisar(); } }
    public int Carisma { get => _player.Carisma; set { CharacterCreator.SetStat(_player, "Carisma", value); Avisar(); } }

    public int PuntosRestantes => CharacterCreator.PuntosRestantes(_player);
    public string MensajeError => CharacterCreator.Validar(_player) ?? string.Empty;
    public bool PuedeComenzar => CharacterCreator.Validar(_player) == null;

    public void Subir(string stat)
    {
        if (CharacterCreator.PuedeSubir(_player, stat))
        {
            CharacterCreator.SetStat(_player, stat, CharacterCreator.GetStat(_player, stat) + 1);
            Avisar();
        }
    }

    public void Bajar(string stat)
    {
        if (CharacterCreator.PuedeBajar(_player, stat))
        {
            CharacterCreator.SetStat(_player, stat, CharacterCreator.GetStat(_player, stat) - 1);
            Avisar();
        }
    }

    public void Reiniciar()
    {
        foreach (var s in CharacterCreator.Stats)
            CharacterCreator.SetStat(_player, s, CharacterCreator.MinStat);
        Avisar();
    }

    public void Aleatorio()
    {
        var rnd = CrearAleatorioConservandoNombreYFoto();
        foreach (var s in CharacterCreator.Stats)
            CharacterCreator.SetStat(_player, s, CharacterCreator.GetStat(rnd, s));
        Avisar();
    }

    private Player CrearAleatorioConservandoNombreYFoto()
    {
        string nombre = string.IsNullOrWhiteSpace(_player.Name) ? "Superviviente" : _player.Name;
        var p = CharacterCreator.CrearAleatorio(nombre);
        p.PhotoPath = _player.PhotoPath;
        return p;
    }

    // Copia para entregar al juego. Evita que la UI siga modificando al jugador.
    public Player BuildPlayer()
    {
        return new Player
        {
            Name = _player.Name.Trim(),
            PhotoPath = _player.PhotoPath,
            Fuerza = _player.Fuerza,
            Destreza = _player.Destreza,
            Resistencia = _player.Resistencia,
            Inteligencia = _player.Inteligencia,
            Percepcion = _player.Percepcion,
            Carisma = _player.Carisma
        };
    }

    private void Avisar()
    {
        OnPropertyChanged(nameof(Nombre));
        OnPropertyChanged(nameof(PhotoPath));
        OnPropertyChanged(nameof(TieneFoto));
        OnPropertyChanged(nameof(Fuerza));
        OnPropertyChanged(nameof(Destreza));
        OnPropertyChanged(nameof(Resistencia));
        OnPropertyChanged(nameof(Inteligencia));
        OnPropertyChanged(nameof(Percepcion));
        OnPropertyChanged(nameof(Carisma));
        OnPropertyChanged(nameof(PuntosRestantes));
        OnPropertyChanged(nameof(MensajeError));
        OnPropertyChanged(nameof(PuedeComenzar));
    }

    private void OnPropertyChanged(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
