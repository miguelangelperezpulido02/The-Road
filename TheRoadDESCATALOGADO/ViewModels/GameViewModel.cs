using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using TheRoad.Logic;
using TheRoad.Models;

namespace TheRoad.ViewModels;

public class ResourceBarViewModel : INotifyPropertyChanged
{
    public string Name { get; }
    public string Icon { get; }
    public int MaxValue { get; }

    private int _value;
    public int Value
    {
        get => _value;
        set
        {
            _value = Math.Clamp(value, 0, MaxValue);
            OnPropertyChanged();
            OnPropertyChanged(nameof(Ratio));
            OnPropertyChanged(nameof(Color));
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    public double Ratio => MaxValue > 0 ? (double)Value / MaxValue : 0;
    public string Color => Ratio > 0.6 ? "#6B8E5C" : Ratio > 0.3 ? "#D8A545" : "#E07A5F";
    public string DisplayText => $"{Value}/{MaxValue}";

    public ResourceBarViewModel(string name, string icon, int maxValue, int initialValue)
    {
        Name = name;
        Icon = icon;
        MaxValue = maxValue;
        Value = initialValue;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class InventoryItemViewModel : INotifyPropertyChanged
{
    public string Name { get; }
    public string Icon { get; }
    public int Count { get; private set; }
    public bool IsUsable { get; }
    public string Category { get; }

    public InventoryItemViewModel(string name, string icon, int count, bool isUsable, string category)
    {
        Name = name;
        Icon = icon;
        Count = count;
        IsUsable = isUsable;
        Category = category;
    }

    public void SetCount(int count)
    {
        Count = count;
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(nameof(CountText));
    }

    public string CountText => Count > 1 ? $"x{Count}" : "";

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class NotificationViewModel : INotifyPropertyChanged
{
    public string Message { get; }
    public NotificationType Type { get; }
    public DateTime Timestamp { get; } = DateTime.Now;

    public NotificationViewModel(string message, NotificationType type = NotificationType.Info)
    {
        Message = message;
        Type = type;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public enum NotificationType
{
    Info,
    Warning,
    Success,
    Danger,
    Scavenge
}

public class GameViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly GameState _state;
    private readonly List<Location> _lugares;
    private readonly List<Route> _rutas;

    private string _vista = "Historia";
    private string _narrativa = string.Empty;
    private string? _destinoSelId;
    private string? _opcionSel;
    private string _modoViaje = "Autopista";

    private readonly ObservableCollection<ResourceBarViewModel> _resourceBars = new();
    private readonly ObservableCollection<InventoryItemViewModel> _inventoryItems = new();
    private readonly ObservableCollection<NotificationViewModel> _notifications = new();
    private readonly ObservableCollection<string> _diarioEntries = new();

    private bool _isTraveling;
    private double _travelProgress;
    private TravelService.TravelResult? _lastTravelResult;

    public GameViewModel(GameState state, List<Location> lugares, List<Route> rutas)
    {
        _state = state;
        _lugares = lugares;
        _rutas = rutas;

        InitializeResourceBars();
        InitializeInventory();
        SyncDiario();

        var actual = LugarActual;
        _narrativa = $"Estás en {actual.Name}.\n{actual.Description}\n\nElige un destino y pulsa Viajar.";
    }

    private void InitializeResourceBars()
    {
        _resourceBars.Add(new ResourceBarViewModel("HP", "❤", 100, _state.Player.HP));
        _resourceBars.Add(new ResourceBarViewModel("Comida", "🍖", 20, _state.Player.Comida));
        _resourceBars.Add(new ResourceBarViewModel("Agua", "💧", 20, _state.Player.Agua));
        _resourceBars.Add(new ResourceBarViewModel("Gasolina", "⛽", 100, _state.Player.Combustible));
        _resourceBars.Add(new ResourceBarViewModel("Medicina", "💊", 10, _state.Player.Medicina));
    }

    private void InitializeInventory()
    {
        var grouped = _state.Player.Inventario
            .GroupBy(x => x)
            .Select(g => new InventoryItemViewModel(
                g.Key,
                GetItemIcon(g.Key),
                g.Count(),
                ItemsService.EsUsable(g.Key),
                GetItemCategory(g.Key)))
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Name)
            .ToList();

        foreach (var item in grouped)
            _inventoryItems.Add(item);
    }

    private static string GetItemIcon(string name) => name switch
    {
        "Bidón de gasolina" => "⛽",
        "Lata de conservas" => "🥫",
        "Vendaje" => "🩹",
        "Chatarra" => "⚙",
        "Navaja" => "🔪",
        _ => "📦"
    };

    private static string GetItemCategory(string name) => name switch
    {
        "Bidón de gasolina" or "Lata de conservas" or "Vendaje" => "Consumibles",
        "Chatarra" or "Navaja" => "Materiales",
        _ => "Otros"
    };

    private void SyncDiario()
    {
        _diarioEntries.Clear();
        foreach (var entry in _state.Diario)
            _diarioEntries.Add(entry);
    }

    private void SyncFromState()
    {
        foreach (var bar in _resourceBars)
        {
            bar.Value = bar.Name switch
            {
                "HP" => _state.Player.HP,
                "Comida" => _state.Player.Comida,
                "Agua" => _state.Player.Agua,
                "Gasolina" => _state.Player.Combustible,
                "Medicina" => _state.Player.Medicina,
                _ => bar.Value
            };
        }

        var grouped = _state.Player.Inventario
            .GroupBy(x => x)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var item in _inventoryItems)
        {
            item.SetCount(grouped.GetValueOrDefault(item.Name, 0));
        }

        SyncDiario();

        OnPropertyChanged(nameof(NombreJugador));
        OnPropertyChanged(nameof(Foto));
        OnPropertyChanged(nameof(DiaActual));
        OnPropertyChanged(nameof(LugarActual));
        OnPropertyChanged(nameof(Stats));
        OnPropertyChanged(nameof(Stats2));
    }

    // --- Resource Bars ---
    public IReadOnlyList<ResourceBarViewModel> ResourceBars => _resourceBars;

    // --- Inventory Grid (8x8 = 64 slots) ---
    public IReadOnlyList<InventoryItemViewModel> InventoryItems => _inventoryItems;
    public int InventoryRows => 8;
    public int InventoryCols => 8;
    public int InventoryCapacity => InventoryRows * InventoryCols;
    public int InventoryUsed => _inventoryItems.Sum(x => x.Count);
    public double InventoryFillRatio => (double)InventoryUsed / InventoryCapacity;

    // --- Notifications ---
    public IReadOnlyList<NotificationViewModel> Notifications => _notifications;

    public void AddNotification(string message, NotificationType type = NotificationType.Info)
    {
        _notifications.Insert(0, new NotificationViewModel(message, type));
        while (_notifications.Count > 10)
            _notifications.RemoveAt(_notifications.Count - 1);
        OnPropertyChanged(nameof(Notifications));
    }

    // --- Travel Animation ---
    public bool IsTraveling
    {
        get => _isTraveling;
        private set { _isTraveling = value; OnPropertyChanged(nameof(IsTraveling)); }
    }

    public double TravelProgress
    {
        get => _travelProgress;
        private set { _travelProgress = value; OnPropertyChanged(nameof(TravelProgress)); }
    }

    public TravelService.TravelResult? LastTravelResult
    {
        get => _lastTravelResult;
        private set { _lastTravelResult = value; OnPropertyChanged(nameof(LastTravelResult)); }
    }

    // --- Basic Properties ---
    public string NombreJugador => _state.Player.Name;
    public string? Foto => _state.Player.PhotoPath;
    public string Stats => $"FUE {_state.Player.Fuerza}  DES {_state.Player.Destreza}  RES {_state.Player.Resistencia}";
    public string Stats2 => $"INT {_state.Player.Inteligencia}  PER {_state.Player.Percepcion}  CAR {_state.Player.Carisma}";
    public string DiaActual => $"Día {_state.Dia} — {LugarActual.Name}";
    public Location LugarActual => _lugares.First(l => l.Id == _state.CurrentLocationId);
    public IReadOnlyList<Location> Lugares => _lugares;
    public IReadOnlyList<Route> Rutas => _rutas;
    public string CurrentId => _state.CurrentLocationId;
    public HashSet<string> Visitados => _state.Visitados;
    public IReadOnlyList<string> Diario => _diarioEntries;

    public string Vista
    {
        get => _vista;
        set { _vista = value; OnPropertyChanged(nameof(Vista)); }
    }

    public string Narrativa
    {
        get => _narrativa;
        private set { _narrativa = value; OnPropertyChanged(nameof(Narrativa)); }
    }

    public string ModoViaje
    {
        get => _modoViaje;
        set
        {
            if (_modoViaje == value) return;
            _modoViaje = value;
            _destinoSelId = null;
            _opcionSel = null;
            RefrescarOpciones();
            OnPropertyChanged(nameof(ModoViaje));
            OnPropertyChanged(nameof(ModoTipo));
            OnPropertyChanged(nameof(EsAutopista));
            OnPropertyChanged(nameof(EsSecundaria));
            OnPropertyChanged(nameof(Destinos));
            OnPropertyChanged(nameof(OpcionSeleccionada));
            OnPropertyChanged(nameof(DestinoSeleccionadoId));
            OnPropertyChanged(nameof(InfoDestino));
            OnPropertyChanged(nameof(PuedeViajar));
        }
    }

    public TipoViaje ModoTipo => _modoViaje == "Secundaria" ? TipoViaje.Segmentado : TipoViaje.Largo;

    public bool EsAutopista
    {
        get => ModoTipo == TipoViaje.Largo;
        set { if (value) ModoViaje = "Autopista"; }
    }

    public bool EsSecundaria
    {
        get => ModoTipo == TipoViaje.Segmentado;
        set { if (value) ModoViaje = "Secundaria"; }
    }

    public ObservableCollection<string> Opciones { get; private set; } = [];

    public string? OpcionSeleccionada
    {
        get => _opcionSel;
        set
        {
            _opcionSel = value;
            var r = RutaDeOpcion(value);
            if (r != null) DestinoSeleccionadoId = TravelService.OtroExtremo(r, _state.CurrentLocationId);
            OnPropertyChanged(nameof(OpcionSeleccionada));
        }
    }

    public List<Route> Destinos => TravelService.DestinosDesde(_state, _rutas, ModoTipo);

    public string? DestinoSeleccionadoId
    {
        get => _destinoSelId;
        set { _destinoSelId = value; OnPropertyChanged(nameof(DestinoSeleccionadoId)); OnPropertyChanged(nameof(InfoDestino)); OnPropertyChanged(nameof(PuedeViajar)); }
    }

    public void SeleccionarDestino(string id)
    {
        if (id == _state.CurrentLocationId) return;
        bool hayModo = HayRuta(id, ModoTipo);
        if (!hayModo && HayRuta(id, TipoViaje.Largo == ModoTipo ? TipoViaje.Segmentado : TipoViaje.Largo))
            ModoViaje = ModoTipo == TipoViaje.Largo ? "Secundaria" : "Autopista";
        DestinoSeleccionadoId = id;
    }

    public string InfoDestino
    {
        get
        {
            if (_destinoSelId == null) return "Selecciona un destino en el mapa o en la lista.";
            var l = _lugares.FirstOrDefault(x => x.Id == _destinoSelId);
            if (l == null) return "Destino no conectado.";
            bool conectado = _rutas.Any(x => Conecta(x, _destinoSelId));
            if (!conectado) return "Destino no conectado.";

            var lineas = new List<string> { $"{l.Name} — riesgo {_rutas.FirstOrDefault(x => Conecta(x, _destinoSelId))?.RiesgoTexto ?? "?"}" };

            var largo = _rutas.FirstOrDefault(x => x.Tipo == TipoViaje.Largo && Conecta(x, _destinoSelId));
            if (largo != null)
                lineas.Add($"Autopista: {largo.DistanceKm} km, -{TravelService.CosteCombustible(largo)} gasolina, 1 día");

            var seg = TravelService.CaminoSegmentado(_state.CurrentLocationId, _destinoSelId, _rutas);
            if (seg.Count > 0)
            {
                int km = seg.Sum(x => x.DistanceKm);
                int gas = seg.Sum(TravelService.CosteCombustible);
                string tramos = seg.Count == 1 ? "1 tramo" : $"{seg.Count} tramos";
                lineas.Add($"Secundaria: {tramos}, {km} km, -{gas} gasolina, {seg.Count} día(s)");
            }

            lineas.Add(l.Description);
            return string.Join("\n", lineas);
        }
    }

    public bool PuedeViajar
    {
        get
        {
            var r = RutaAlDestino();
            return r != null && TravelService.PuedeViajar(_state, r) == null;
        }
    }

    public void RefrescarOpciones()
    {
        Opciones = new ObservableCollection<string>(Destinos.Select(r =>
        {
            string otro = TravelService.OtroExtremo(r, _state.CurrentLocationId);
            var l = _lugares.First(x => x.Id == otro);
            int gas = TravelService.CosteCombustible(r);

            if (r.Tipo == TipoViaje.Largo)
                return $"{l.Name} — autopista {r.DistanceKm} km · -{gas} gas · 1 día · riesgo {r.RiesgoTexto}";

            string extra = string.Empty;
            string? final = CiudadAlFinal(otro, _state.CurrentLocationId);
            if (final != null && final != otro)
                extra = $" → {_lugares.First(x => x.Id == final).Name}";
            return $"{l.Name} — secundaria {r.DistanceKm} km{extra} · -{gas} gas · 1 día · riesgo {r.RiesgoTexto}";
        }));
        OnPropertyChanged(nameof(Opciones));
    }

    public async void Viajar()
    {
        var r = RutaAlDestino() ?? RutaDeOpcion(_opcionSel);
        if (r == null) { Narrativa = "Selecciona un destino primero."; return; }

        string? bloqueo = TravelService.PuedeViajar(_state, r);
        if (bloqueo != null) { Narrativa = bloqueo; return; }

        IsTraveling = true;
        TravelProgress = 0;

        // Animar progreso
        for (int i = 0; i <= 100; i += 5)
        {
            TravelProgress = i / 100.0;
            await Task.Delay(15);
        }

        var result = TravelService.Viajar(_state, r, _lugares);
        LastTravelResult = result;

        Narrativa = result.Narrative;
        _destinoSelId = null;
        _opcionSel = null;
        RefrescarOpciones();
        SyncFromState();

        // Notificaciones por eventos
        foreach (var evt in result.Events)
        {
            var type = evt.Contains("encuentras") || evt.Contains("hallan") || evt.Contains("+") ? NotificationType.Success :
                       evt.Contains("pierdes") || evt.Contains("roba") || evt.Contains("-") ? NotificationType.Warning :
                       NotificationType.Info;
            AddNotification(evt, type);
        }

        if (_state.Player.HP <= 0)
        {
            Narrativa += "\n\nHas muerto. El viaje termina aquí.";
            AddNotification("Has muerto. Fin del viaje.", NotificationType.Danger);
        }

        IsTraveling = false;
        TravelProgress = 0;
    }

    public void UsarItem(string? item)
    {
        if (string.IsNullOrEmpty(item)) return;
        string? texto = ItemsService.Usar(item, _state.Player);
        if (texto != null)
        {
            Narrativa = texto;
            AddNotification(texto, NotificationType.Success);
            SyncFromState();
        }
        else
        {
            AddNotification($"{item}: no se puede usar.", NotificationType.Warning);
        }
    }

    public void DebugGasolina(int cantidad)
    {
        _state.Player.Combustible += cantidad;
        AddNotification($"[DEBUG] Depósito +{cantidad} gasolina.", NotificationType.Info);
        SyncFromState();
    }

    public void DebugBidon()
    {
        _state.Player.Inventario.Add("Bidón de gasolina");
        AddNotification("[DEBUG] Bidón añadido al inventario.", NotificationType.Info);
        SyncFromState();
    }

    private bool HayRuta(string destinoId, TipoViaje modo)
        => _rutas.Any(x => x.Tipo == modo && Conecta(x, destinoId));

    private bool Conecta(Route r, string destinoId)
        => (r.FromId == _state.CurrentLocationId && r.ToId == destinoId) ||
           (r.ToId == _state.CurrentLocationId && r.FromId == destinoId);

    private string? CiudadAlFinal(string desde, string evitando)
    {
        string? previo = evitando;
        string actual = desde;
        for (int i = 0; i < 6; i++)
        {
            var lug = _lugares.First(x => x.Id == actual);
            if (!lug.EsParada) return actual;

            string? sig = _rutas
                .Where(x => x.Tipo == TipoViaje.Segmentado && (x.FromId == actual || x.ToId == actual))
                .Select(x => TravelService.OtroExtremo(x, actual))
                .FirstOrDefault(x => x != previo);
            if (sig == null) return null;
            previo = actual;
            actual = sig;
        }
        return null;
    }

    private Route? RutaAlDestino()
    {
        if (_destinoSelId == null) return null;
        return _rutas.FirstOrDefault(x => x.Tipo == ModoTipo && Conecta(x, _destinoSelId));
    }

    private Route? RutaDeOpcion(string? opcion)
    {
        if (opcion == null) return null;
        foreach (var r in Destinos)
        {
            string otro = TravelService.OtroExtremo(r, _state.CurrentLocationId);
            var l = _lugares.First(x => x.Id == otro);
            if (opcion.StartsWith(l.Name)) return r;
        }
        return null;
    }

    private void OnPropertyChanged(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}