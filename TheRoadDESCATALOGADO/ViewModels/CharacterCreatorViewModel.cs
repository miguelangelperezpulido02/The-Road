using System.Collections.ObjectModel;
using System.ComponentModel;
using TheRoad.Logic;
using TheRoad.Models;

namespace TheRoad.ViewModels;

public class StatSegment : INotifyPropertyChanged
{
    private bool _isFilled;
    public int Index { get; }
    public bool IsFilled
    {
        get => _isFilled;
        set { _isFilled = value; OnPropertyChanged(); }
    }
    public StatSegment(int index) => Index = index;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class StatRowViewModel : INotifyPropertyChanged
{
    public string Name { get; }
    public string Icon { get; }
    public string Description { get; }
    public ObservableCollection<StatSegment> Segments { get; } = new();

    private int _value;
    public int Value
    {
        get => _value;
        set
        {
            _value = value;
            UpdateSegments();
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanIncrease));
            OnPropertyChanged(nameof(CanDecrease));
        }
    }

    public bool CanIncrease => Value < CharacterCreator.MaxStat && PuntosRestantes > 0;
    public bool CanDecrease => Value > CharacterCreator.MinStat;

    public int PuntosRestantes { get; set; }

    public StatRowViewModel(string name, string icon, string description, int initialValue, int puntosRestantes)
    {
        Name = name;
        Icon = icon;
        Description = description;
        PuntosRestantes = puntosRestantes;
        for (int i = 0; i < CharacterCreator.MaxStat; i++)
            Segments.Add(new StatSegment(i));
        Value = initialValue;
    }

    public void RefreshCanExecute(int puntosRestantes)
    {
        PuntosRestantes = puntosRestantes;
        OnPropertyChanged(nameof(CanIncrease));
        OnPropertyChanged(nameof(CanDecrease));
    }

    private void UpdateSegments()
    {
        for (int i = 0; i < Segments.Count; i++)
            Segments[i].IsFilled = i < Value;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class CharacterCreatorViewModel : INotifyPropertyChanged
{
    private readonly Player _player = new();
    private readonly ObservableCollection<StatRowViewModel> _statRows = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<StatRowViewModel> StatRows => _statRows;

    public string Nombre
    {
        get => _player.Name;
        set { _player.Name = value?.TrimStart() ?? ""; Avisar(); }
    }

    public string? PhotoPath
    {
        get => _player.PhotoPath;
        set { _player.PhotoPath = value; Avisar(); }
    }

    public bool TieneFoto => !string.IsNullOrWhiteSpace(_player.PhotoPath);

    public int Fuerza
    {
        get => _player.Fuerza;
        set { CharacterCreator.SetStat(_player, "Fuerza", value); Avisar(); }
    }

    public int Destreza
    {
        get => _player.Destreza;
        set { CharacterCreator.SetStat(_player, "Destreza", value); Avisar(); }
    }

    public int Resistencia
    {
        get => _player.Resistencia;
        set { CharacterCreator.SetStat(_player, "Resistencia", value); Avisar(); }
    }

    public int Inteligencia
    {
        get => _player.Inteligencia;
        set { CharacterCreator.SetStat(_player, "Inteligencia", value); Avisar(); }
    }

    public int Percepcion
    {
        get => _player.Percepcion;
        set { CharacterCreator.SetStat(_player, "Percepcion", value); Avisar(); }
    }

    public int Carisma
    {
        get => _player.Carisma;
        set { CharacterCreator.SetStat(_player, "Carisma", value); Avisar(); }
    }

    public int PuntosRestantes => CharacterCreator.PuntosRestantes(_player);
    public string MensajeError => CharacterCreator.Validar(_player) ?? string.Empty;
    public bool PuedeComenzar => CharacterCreator.Validar(_player) == null;

    public CharacterCreatorViewModel()
    {
        InitializeStatRows();
    }

    private void InitializeStatRows()
    {
        var stats = new[]
        {
            new { Name = "Fuerza", Icon = "⚔", Desc = "Combate cuerpo a cuerpo y mover obstáculos." },
            new { Name = "Destreza", Icon = "🏃", Desc = "Huir, sigilo y acciones rápidas." },
            new { Name = "Resistencia", Icon = "🛡", Desc = "Aguantar hambre, heridas y marchas largas." },
            new { Name = "Inteligencia", Icon = "🧠", Desc = "Medicina, mecánica y resolver problemas." },
            new { Name = "Percepción", Icon = "👁", Desc = "Detectar peligros y encontrar recursos." },
            new { Name = "Carisma", Icon = "💬", Desc = "Persuadir y comerciar con otros supervivientes." }
        };

        foreach (var s in stats)
        {
            var row = new StatRowViewModel(s.Name, s.Icon, s.Desc,
                CharacterCreator.GetStat(_player, s.Name), PuntosRestantes);
            _statRows.Add(row);
        }
    }

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
        int restantes = PuntosRestantes;
        foreach (var row in _statRows)
        {
            // Actualizar Value desde el Player real
            row.Value = CharacterCreator.GetStat(_player, row.Name);
            row.RefreshCanExecute(restantes);
        }

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