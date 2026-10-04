using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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

    private string _vista = "Vista";
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
    }

    private void InitializeInventory() => RebuildInventory();

    /// <summary>Reconstruye las 64 celdas: cada instancia de objeto ocupa una celda física.</summary>
    private void RebuildInventory()
    {
        _inventoryItems.Clear();

        foreach (var group in _state.Player.Inventario
            .Where(x => !string.IsNullOrEmpty(x)) // Entrada malformada: se ignora.
            .GroupBy(x => x)
            .OrderBy(g => GetItemCategory(g.Key))
            .ThenBy(g => g.Key))
        {
            for (int i = 0; i < group.Count() && _inventoryItems.Count < InventoryCapacity; i++)
                _inventoryItems.Add(new InventoryItemViewModel(
                    group.Key,
                    GetItemIcon(group.Key),
                    1,
                    ItemsService.EsUsable(group.Key),
                    GetItemCategory(group.Key)));
        }

        PadInventory();
    }

    private void PadInventory()
    {
        while (_inventoryItems.Count < InventoryCapacity)
            _inventoryItems.Add(new InventoryItemViewModel("", "", 0, false, ""));
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
                _ => bar.Value
            };
        }

        RebuildInventory();

        SyncDiario();

        OnPropertyChanged(nameof(NombreJugador));
        OnPropertyChanged(nameof(NivelJugador));
        OnPropertyChanged(nameof(Foto));
        OnPropertyChanged(nameof(DiaActual));
        OnPropertyChanged(nameof(LugarActual));
        OnPropertyChanged(nameof(Stats));
        OnPropertyChanged(nameof(Stats2));
        OnPropertyChanged(nameof(VehiculoEstado));
        OnPropertyChanged(nameof(VehiculoTexto));
        OnPropertyChanged(nameof(ProbabilidadAveria));
        OnPropertyChanged(nameof(ProbabilidadAveriaTexto));
        OnPropertyChanged(nameof(ChatarraCount));
        OnPropertyChanged(nameof(PuedeReparar));
        OnPropertyChanged(nameof(PuedeRebuscar));
        OnPropertyChanged(nameof(EstaMuerto));
        OnPropertyChanged(nameof(HaGanado));
        OnPropertyChanged(nameof(PuedeViajar));
    }

    // --- Resource Bars ---
    public IReadOnlyList<ResourceBarViewModel> ResourceBars => _resourceBars;

    // --- Vehículo ---
    public int VehiculoEstado => _state.Vehiculo;
    public string VehiculoTexto => $"{_state.Vehiculo}/{VehicleService.MaxEstado}";
    public int ChatarraCount => VehicleService.ChatarraCount(_state.Player);
    public bool PuedeReparar => _state.Vehiculo < VehicleService.MaxEstado
        && VehicleService.ChatarraCount(_state.Player) > 0
        && !_state.EstaMuerto && !_state.HaGanado;

    public bool EstaMuerto => _state.EstaMuerto;
    public bool HaGanado => _state.HaGanado;

    // --- Avería: probabilidad visible en % ---
    public int ProbabilidadAveria => VehicleService.ProbabilidadAveria(_state.Vehiculo);
    public string ProbabilidadAveriaTexto => $"{ProbabilidadAveria}%";

    // --- Popup DEBUG genérico: un único popup reutilizable para avería/eventos/lo que haga falta ---
    private bool _popupVisible;
    public bool PopupVisible
    {
        get => _popupVisible;
        private set { _popupVisible = value; OnPropertyChanged(nameof(PopupVisible)); }
    }

    private string _popupTitulo = string.Empty;
    public string PopupTitulo
    {
        get => _popupTitulo;
        private set { _popupTitulo = value; OnPropertyChanged(nameof(PopupTitulo)); }
    }

    private string _popupTexto = string.Empty;
    public string PopupTexto
    {
        get => _popupTexto;
        private set { _popupTexto = value; OnPropertyChanged(nameof(PopupTexto)); }
    }

    public void CerrarPopup()
    {
        if (HayDecision) return; // Con decisión pendiente hay que elegir: no se cierra.
        PopupVisible = false;
    }

    // --- Eventos con decisión: el viaje deja situaciones pendientes y el jugador elige ---
    // Un viaje puede traer dos (ruta + parada): se resuelven en orden con una cola.
    public class OpcionDecisionItem
    {
        public int Indice { get; set; }
        public string Titulo { get; set; } = "";
        public bool Habilitada { get; set; } = true;
    }

    private readonly Queue<(DataService.DecisionDto Dec, bool EsDebug)> _colaDecisiones = new();

    public DataService.DecisionDto? DecisionPendiente =>
        _colaDecisiones.Count > 0 ? _colaDecisiones.Peek().Dec : null;

    public bool HayDecision => _colaDecisiones.Count > 0;
    public bool SinDecision => !HayDecision;

    public ObservableCollection<OpcionDecisionItem> OpcionesDecision { get; } = new();

    private void NotificarCola()
    {
        OnPropertyChanged(nameof(DecisionPendiente));
        OnPropertyChanged(nameof(HayDecision));
        OnPropertyChanged(nameof(SinDecision));
    }

    /// <summary>Recibe las decisiones pendientes de un viaje (gameplay, en orden).</summary>
    public void RecibirDecisiones(IEnumerable<DataService.DecisionDto> decisiones)
    {
        foreach (var dec in decisiones)
            MostrarDecision(dec);
    }

    private void MostrarDecision(DataService.DecisionDto dec, bool esDebug = false)
    {
        _colaDecisiones.Enqueue((dec, esDebug));
        if (_colaDecisiones.Count == 1)
            PresentarActual();
        NotificarCola();
    }

    private void PresentarActual()
    {
        var dec = DecisionPendiente;
        if (dec == null) return;
        OpcionesDecision.Clear();
        for (int i = 0; i < dec.Options.Count; i++)
        {
            var op = dec.Options[i];
            if (!Eventos.CumpleMarcas(_state, op)) continue; // Marcas: se oculta, no se deshabilita.
            bool okRecurso = Eventos.CumpleRecurso(_state, op);
            string titulo = string.IsNullOrEmpty(op.Hint) ? op.Text : $"{op.Text}\n({op.Hint})";
            if (!okRecurso && op.Requiere != null)
                titulo += $" (requiere {op.Requiere.Min} {Eventos.NombreRecurso(op.Requiere.Recurso)})";
            OpcionesDecision.Add(new OpcionDecisionItem { Indice = i, Titulo = titulo, Habilitada = okRecurso });
        }
        PopupTitulo = "🎲 DECISIÓN";
        PopupTexto = dec.Text;
        PopupVisible = true;
    }

    public void ElegirOpcion(int indice)
    {
        if (_colaDecisiones.Count == 0) return;
        var (dec, esDebug) = _colaDecisiones.Peek();
        if (indice < 0 || indice >= dec.Options.Count) return;
        if (!Eventos.OpcionDisponible(_state, dec.Options[indice]))
        {
            AddNotification("No cumples los requisitos de esa opción.", NotificationType.Warning);
            return;
        }
        string? txt = Eventos.ResolverDecision(_state, dec, indice);
        if (txt == null) return;
        Narrativa += "\n" + txt;
        if (!esDebug)
            _state.AnadirDiario($"🎲 {txt}");
        AddNotification(txt, NotificationType.Info);
        _colaDecisiones.Dequeue();
        if (_colaDecisiones.Count > 0)
            PresentarActual();
        else
            OpcionesDecision.Clear();
        NotificarCola();
        SyncFromState();
        CerrarPopup();
    }

    /// <summary>Fuerza una decisión disponible (sin diario, como el resto de DEBUG).
    /// grande=true: del pool grande (botón eventoG); false: del pool de Segmentado.</summary>
    public void DebugForzarDecision(bool grande = false)
    {
        DataService.DecisionDto? dec = grande
            ? Eventos.DecisionGrande()
            : GameData.Events.DecisionEvents
                .FirstOrDefault(d => d != null && !string.IsNullOrEmpty(d.Text) && d.Options.Count > 0);
        if (dec == null)
        {
            AddNotification("[DEBUG] No hay eventos con decisión disponibles.", NotificationType.Warning);
            return;
        }
        MostrarDecision(dec, esDebug: true);
        AddNotification("[DEBUG] Decisión forzada y pendiente de elegir.", NotificationType.Info);
    }

    // --- Debug: botones para testear en profundidad (la barra solo es visible en DEBUG) ---
    public class DebugAccion
    {
        public string Etiqueta { get; set; } = "";
        public string Tag { get; set; } = "";
        public string? Tooltip { get; set; }
    }

    /// <summary>Botones de la barra DEBUG (data-driven: añadir uno no toca el XAML).</summary>
    public ObservableCollection<DebugAccion> AccionesDebug { get; } = new()
    {
        new() { Etiqueta = "Forzar avería", Tag = "averia", Tooltip = "Aplica -10 de salud y abre el popup de avería" },
        new() { Etiqueta = "Salud 100", Tag = "s100", Tooltip = "Vehículo al 100%: probabilidad 0%" },
        new() { Etiqueta = "Salud 50", Tag = "s50", Tooltip = "Vehículo al 50%: probabilidad 15%" },
        new() { Etiqueta = "Salud 1", Tag = "s1", Tooltip = "Vehículo casi muerto: probabilidad casi máxima" },
        new() { Etiqueta = "Salud 0", Tag = "s0", Tooltip = "Vehículo roto: viajar bloqueado, probabilidad 30%" },
        new() { Etiqueta = "Tirada avería", Tag = "tirada", Tooltip = "Tira la probabilidad actual y enseña el resultado (sin daño)" },
        new() { Etiqueta = "🎲 Evento grande", Tag = "eventoG", Tooltip = "Fuerza una decisión grande (elige opción; sin diario)" },
        new() { Etiqueta = "🎲 Evento menor", Tag = "eventoM", Tooltip = "Fuerza una decisión (elige opción; sin diario)" },
        new() { Etiqueta = "🎲 Decisión", Tag = "decisionD", Tooltip = "Fuerza la gasolinera (elige opción; sin diario)" },
        new() { Etiqueta = "+20 gasolina", Tag = "gas" },
        new() { Etiqueta = "+Bidón", Tag = "bidon" },
        new() { Etiqueta = "🌙 Noche 22:00", Tag = "hora22", Tooltip = "Fija el reloj a las 22:00 (prueba eventos nocturnos)" },
        new() { Etiqueta = "☀️ Día 08:00", Tag = "hora8", Tooltip = "Fija el reloj a las 08:00 (prueba eventos diurnos)" },
    };
    public bool EsDebugBuild
    {
        get
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }

    /// <summary>Fuerza una avería real: -10 de salud + popup, sin depender del azar.</summary>
    public void DebugForzarAveria()
    {
        _state.Vehiculo = Math.Max(0, _state.Vehiculo - VehicleService.DanoAveria);
        PopupTitulo = "⚠ AVERÍA (DEBUG)";
        PopupTexto =
            $"⚠ Avería forzada (debug): -{VehicleService.DanoAveria} vehículo.\n" +
            $"Salud: {VehiculoTexto} · Próximo viaje: {ProbabilidadAveria}%";
        PopupVisible = true;
        SyncFromState();
        AddNotification($"[DEBUG] Avería forzada: {VehiculoTexto}.", NotificationType.Warning);
    }

    /// <summary>Fija la salud del vehículo para probar la curva de probabilidad.</summary>
    public void DebugSaludVehiculo(int salud)
    {
        _state.Vehiculo = Math.Clamp(salud, 0, VehicleService.MaxEstado);
        SyncFromState();
        AddNotification($"[DEBUG] Salud vehículo {VehiculoTexto} · próximo viaje {ProbabilidadAveria}%.",
            NotificationType.Info);
    }

    /// <summary>Tira la probabilidad actual y enseña el resultado, sin aplicar daño.</summary>
    public void DebugTiradaAveria()
    {
        int prob = ProbabilidadAveria;
        bool averia = VehicleService.HayAveria(_state.Vehiculo);
        AddNotification(
            $"[DEBUG] Tirada avería: prob {prob}% → {(averia ? "AVERÍA (sin daño aplicado)" : "sin avería")}.",
            averia ? NotificationType.Warning : NotificationType.Info);
    }

    /// <summary>Fija el reloj para probar eventos de día/noche. No toca el día:
    /// viajar después puede cruzar la medianoche y sumar un día lateral.</summary>
    public void DebugFijarHora(int hora)
    {
        _state.Hora = Math.Clamp(hora, 0, 23);
        SyncFromState();
        AddNotification($"[DEBUG] Reloj a las {_state.Hora:00}:00 ({(_state.EsDeNoche ? "noche" : "día")}).",
            NotificationType.Info);
    }

    public void RepararVehiculo()
    {
        if (_state.EstaMuerto) { Narrativa = "Estás muerto. El coche ya no te lleva a ninguna parte."; return; }
        if (_state.HaGanado) { Narrativa = "Llegaste a Nashville. El viaje terminó."; return; }
        string? texto = VehicleService.Reparar(_state.Player, _state);
        if (texto != null)
        {
            Narrativa = texto;
            AddNotification(texto, NotificationType.Success);
            SyncFromState();
        }
        else
        {
            AddNotification("No tienes chatarra para reparar.", NotificationType.Warning);
        }
    }

    // --- Inventory Grid (8x8 = 64 slots) ---
    public IReadOnlyList<InventoryItemViewModel> InventoryItems => _inventoryItems;
    public int InventoryRows => 8;
    public int InventoryCols => 8;
    public int InventoryCapacity => InventoryRows * InventoryCols;
    public int InventoryUsed => _inventoryItems.Count(x => !string.IsNullOrEmpty(x.Name));
    public double InventoryFillRatio => (double)InventoryUsed / InventoryCapacity;

    // --- Notifications ---
    public IReadOnlyList<NotificationViewModel> Notifications => _notifications;

    private CancellationTokenSource _notifCts = new();

    public void AddNotification(string message, NotificationType type = NotificationType.Info)
    {
        var notification = new NotificationViewModel(message, type);
        _notifications.Insert(0, notification);
        while (_notifications.Count > 10)
            _notifications.RemoveAt(_notifications.Count - 1);
        OnPropertyChanged(nameof(Notifications));

        // Si se canceló al cerrar la ventana, se reabre el grifo para la siguiente.
        if (_notifCts.IsCancellationRequested) _notifCts = new CancellationTokenSource();
        var token = _notifCts.Token;

        // Se auto-descarta pasados unos segundos, tolerando el cierre de la app.
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), token);
                Application.Current?.Dispatcher.InvokeAsync(() =>
                {
                    if (_notifications.Remove(notification))
                        OnPropertyChanged(nameof(Notifications));
                });
            }
            catch (OperationCanceledException)
            {
                // Ventana cerrada: la notificación ya no importa.
            }
            catch
            {
                // App cerrándose: la notificación ya no importa.
            }
        });
    }

    /// <summary>Cancela los auto-descartes pendientes (al cerrar la ventana).</summary>
    public void CancelarNotificaciones() => _notifCts.Cancel();

    // --- Travel Animation ---
    public bool IsTraveling
    {
        get => _isTraveling;
        private set { _isTraveling = value; OnPropertyChanged(nameof(IsTraveling)); OnPropertyChanged(nameof(PuedeViajar)); }
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
    public int NivelJugador => _state.Player.Level;
    public string? Foto => _state.Player.PhotoPath;
    public string Stats => $"FUE {_state.Player.Fuerza}  DES {_state.Player.Destreza}  RES {_state.Player.Resistencia}";
    public string Stats2 => $"INT {_state.Player.Inteligencia}  PER {_state.Player.Percepcion}  CAR {_state.Player.Carisma}";
    public string DiaActual => $"Día {_state.Dia} · {_state.Hora:00}:00 · {(_state.EsDeNoche ? "NOCHE" : "DÍA")} — {LugarActual.Name}";
    public Location LugarActual
        => _lugares.FirstOrDefault(l => l.Id == _state.CurrentLocationId)
            ?? _lugares.FirstOrDefault()
            ?? Location.Unknown;
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

    public List<Route> Destinos => TravelService.DestinosDesde(_state, _rutas, _lugares, ModoTipo);

    public string? DestinoSeleccionadoId
    {
        get => _destinoSelId;
        set { _destinoSelId = value; OnPropertyChanged(nameof(DestinoSeleccionadoId)); OnPropertyChanged(nameof(InfoDestino)); OnPropertyChanged(nameof(PuedeViajar)); }
    }

    public void SeleccionarDestino(string id)
    {
        if (id == _state.CurrentLocationId) return;
        if (_state.HaGanado) { Narrativa = "Llegaste a Nashville. El viaje terminó."; return; }
        if (!EsDestinoValido(id)) { Narrativa = "Solo puedes avanzar hacia el oeste. El este queda atrás."; return; }
        bool hayModo = HayRuta(id, ModoTipo);
        if (!hayModo && HayRuta(id, TipoViaje.Largo == ModoTipo ? TipoViaje.Segmentado : TipoViaje.Largo))
            ModoViaje = ModoTipo == TipoViaje.Largo ? "Secundaria" : "Autopista";
        DestinoSeleccionadoId = id;
    }

    /// <summary>Destino seleccionable: conectado por ruta y al oeste (sin retornos).</summary>
    private bool EsDestinoValido(string destinoId)
    {
        var actual = _lugares.FirstOrDefault(x => x.Id == _state.CurrentLocationId);
        var dest = _lugares.FirstOrDefault(x => x.Id == destinoId);
        return actual != null && dest != null
            && TravelService.EsAvance(actual, dest)
            && _rutas.Any(x => Conecta(x, destinoId));
    }

    public string InfoDestino
    {
        get
        {
            if (_destinoSelId == null) return "Selecciona un destino en el mapa o en la lista.";
            var l = _lugares.FirstOrDefault(x => x.Id == _destinoSelId);
            if (l == null) return "Destino no conectado.";
            if (!EsDestinoValido(_destinoSelId)) return "Destino no conectado.";

            var lineas = new List<string> { $"{l.Name} — riesgo {_rutas.FirstOrDefault(x => Conecta(x, _destinoSelId))?.RiesgoTexto ?? "?"}" };

            var largo = _rutas.FirstOrDefault(x => x.Tipo == TipoViaje.Largo && Conecta(x, _destinoSelId));
            if (largo != null)
                lineas.Add($"Autopista: {largo.DistanceKm} km, -{TravelService.CosteCombustible(largo)} gasolina, {TravelService.TiempoViajeTxt(largo)} · eventos {TravelService.ProbabilidadEvento(largo)}% · rápida y peligrosa");

            var seg = TravelService.CaminoSegmentado(_state.CurrentLocationId, _destinoSelId, _rutas, _lugares);
            if (seg.Count > 0)
            {
                int km = seg.Sum(x => x.DistanceKm);
                int gas = seg.Sum(TravelService.CosteCombustible);
                string tramos = seg.Count == 1 ? "1 tramo" : $"{seg.Count} tramos";
                // Cada tramo rueda con su propia probabilidad: rango solo si difieren de verdad.
                var probs = seg.Select(TravelService.ProbabilidadEvento).ToList();
                string eventosTxt = probs.Count == 1 || probs.Min() == probs.Max()
                    ? $"eventos {probs[0]}%"
                    : $"eventos {probs.Min()}–{probs.Max()}%";
                lineas.Add($"Secundaria: {tramos}, {km} km, -{gas} gasolina, {TravelService.TiempoViajeTxt(seg)} · {eventosTxt} · lenta con oportunidades");
            }

            lineas.Add(l.Description);
            return string.Join("\n", lineas);
        }
    }

    public bool PuedeViajar
    {
        get
        {
            if (IsTraveling) return false;
            if (HayDecision) return false; // Hay que elegir opción antes de seguir viajando.
            var r = RutaAlDestino();
            return r != null && TravelService.PuedeViajar(_state, r) == null;
        }
    }

    public void RefrescarOpciones()
    {
        var lista = new List<string>();
        foreach (var r in Destinos)
        {
            string otro = TravelService.OtroExtremo(r, _state.CurrentLocationId);
            var l = _lugares.FirstOrDefault(x => x.Id == otro);
            if (l == null) continue; // Ruta con extremo desconocido: se omite sin tumbar la lista.
            int gas = TravelService.CosteCombustible(r);

            if (r.Tipo == TipoViaje.Largo)
            {
                lista.Add($"{l.Name} — autopista {r.DistanceKm} km · -{gas} gas · {TravelService.TiempoViajeTxt(r)} · riesgo {r.RiesgoTexto} · eventos {TravelService.ProbabilidadEvento(r)}% · rápida y peligrosa");
                continue;
            }

            string extra = string.Empty;
            string? final = CiudadAlFinal(otro, _state.CurrentLocationId);
            if (final != null && final != otro)
            {
                string? nombreFinal = _lugares.FirstOrDefault(x => x.Id == final)?.Name;
                if (nombreFinal != null) extra = $" → {nombreFinal}";
            }
            lista.Add($"{l.Name} — secundaria {r.DistanceKm} km{extra} · -{gas} gas · {TravelService.TiempoViajeTxt(r)} · riesgo {r.RiesgoTexto} · eventos {TravelService.ProbabilidadEvento(r)}% · lenta con oportunidades");
        }
        Opciones = new ObservableCollection<string>(lista);
        OnPropertyChanged(nameof(Opciones));
    }

    public async Task ViajarAsync()
    {
        var r = RutaAlDestino() ?? RutaDeOpcion(_opcionSel);
        if (r == null) { Narrativa = "Selecciona un destino primero."; return; }

        string? bloqueo = TravelService.PuedeViajar(_state, r);
        if (bloqueo != null) { Narrativa = bloqueo; return; }

        if (IsTraveling) return; // Doble clic: el viaje en curso manda.
        IsTraveling = true;
        TravelProgress = 0;

        try
        {
            // Animar progreso
            for (int i = 0; i <= 100; i += 5)
            {
                TravelProgress = i / 100.0;
                await Task.Delay(15);
            }

            var result = TravelService.Viajar(_state, r, _lugares);
            LastTravelResult = result;

            Narrativa = result.Narrative;
            DestinoSeleccionadoId = null;
            OpcionSeleccionada = null;
            RefrescarOpciones();
            OnPropertyChanged(nameof(Destinos));
            SyncFromState();

#if DEBUG
            // Popup DEBUG del viaje: solo la avería lo abre (los eventos con decisión
            // tienen su propio popup de gameplay con opciones).
            if (result.HuboAveria)
            {
                PopupTitulo = "⚠ AVERÍA (DEBUG)";
                PopupTexto =
                    $"⚠ Avería en ruta: el motor tose y pierdes piezas por el camino (-{VehicleService.DanoAveria} vehículo).\n" +
                    $"Salud: {VehiculoTexto} · Prob. al salir: {result.ProbAveria}% · Próximo viaje: {ProbabilidadAveria}%";
                PopupVisible = true;
            }
#endif

            // Decisión pendiente de gameplay: popup con opciones (siempre, no solo DEBUG).
            RecibirDecisiones(result.DecisionesPendientes);

            // Notificaciones por eventos (tono tipado en origen, sin parsear texto).
            foreach (var aviso in result.Events)
            {
                var type = aviso.Tono switch
                {
                    EventoTono.Bueno => NotificationType.Success,
                    EventoTono.Malo => NotificationType.Warning,
                    EventoTono.Decision => NotificationType.Scavenge,
                    _ => NotificationType.Info,
                };
                AddNotification(aviso.Texto, type);
            }

            if (_state.EstaMuerto)
            {
                string lugar = _lugares.FirstOrDefault(x => x.Id == _state.CurrentLocationId)?.Name ?? "la carretera";
                Narrativa += "\n\nHas muerto. El viaje termina aquí.";
                PopupTitulo = "☠ FIN DEL VIAJE";
                PopupTexto = $"Has muerto el día {_state.Dia} a las {_state.Hora:00}:00, en {lugar}.\nEl camino sigue sin ti.";
                PopupVisible = true;
                AddNotification("Has muerto. Fin del viaje.", NotificationType.Danger);
            }
            else if (_state.HaGanado)
            {
                Narrativa += "\n\nHas llegado a Nashville. El viaje termina aquí.";
                PopupTitulo = "🏁 FIN DEL VIAJE";
                PopupTexto = $"Has llegado a Nashville el día {_state.Dia} a las {_state.Hora:00}:00.\nEl camino sigue sin ti... pero el tuyo terminó.";
                PopupVisible = true;
                AddNotification("Llegaste a Nashville. Fin del viaje.", NotificationType.Success);
            }

            if (_state.Vehiculo <= 0)
                AddNotification("El coche no anda (0/100). Repáralo con chatarra.", NotificationType.Danger);
            else if (_state.Vehiculo < VehicleService.UmbralAviso)
                AddNotification($"⚠ El coche está en las últimas ({VehiculoTexto}).", NotificationType.Warning);
        }
        catch (Exception ex)
        {
            Narrativa = "Error durante el viaje. El grupo se detiene a revisar el coche.";
            AddNotification($"Error de viaje: {ex.Message}", NotificationType.Danger);
        }
        finally
        {
            IsTraveling = false;
            TravelProgress = 0;
        }
    }

    /// <summary>Anti-softlock: con el coche a 0, rebuscar los alrededores a pie (4 h) como decisión.</summary>
    public bool PuedeRebuscar => _state.Vehiculo <= 0 && !_state.EstaMuerto && !_state.HaGanado;

    public void RebuscarAlrededores()
    {
        if (!PuedeRebuscar) return;
        _state.AvanzarHoras(4);
        var dec = Eventos.DecisionRebusca(_state);
        _state.AnadirDiario("🔧 Rebuscas los alrededores a pie (4 h).");
        if (dec != null) RecibirDecisiones(new[] { dec });
        else Narrativa = "Rebuscas los alrededores durante horas y no encuentras nada útil.";
        SyncFromState();
    }

    public void UsarItem(string? item)
    {
        if (string.IsNullOrEmpty(item)) return;
        if (_state.EstaMuerto) { Narrativa = "Estás muerto. Los objetos ya no te sirven."; return; }
        if (_state.HaGanado) { Narrativa = "Llegaste a Nashville. El viaje terminó."; return; }
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
        _state.Player.Combustible = Math.Min(ItemsService.MaxCombustible, _state.Player.Combustible + cantidad);
        AddNotification($"[DEBUG] Depósito +{cantidad} gasolina.", NotificationType.Info);
        SyncFromState();
    }

    public void DebugBidon()
    {
        if (_state.Player.Inventario.Count >= ItemsService.Capacidad)
        {
            AddNotification("[DEBUG] Inventario lleno.", NotificationType.Warning);
            return;
        }
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
            var lug = _lugares.FirstOrDefault(x => x.Id == actual);
            if (lug == null) return null; // Id desconocido: sin ciudad final.
            if (!lug.EsParada) return actual;

            string? sig = _rutas
                .Where(x => x.Tipo == TipoViaje.Segmentado && (x.FromId == actual || x.ToId == actual))
                .Select(x => TravelService.OtroExtremo(x, actual))
                .Where(x => x != previo && EsMasAlOeste(x, actual))
                .FirstOrDefault();
            if (sig == null) return null;
            previo = actual;
            actual = sig;
        }
        return null;
    }

    private bool EsMasAlOeste(string id, string que)
    {
        var a = _lugares.FirstOrDefault(x => x.Id == id);
        var b = _lugares.FirstOrDefault(x => x.Id == que);
        return a != null && b != null && a.X < b.X;
    }

    private Route? RutaAlDestino()
    {
        if (_destinoSelId == null) return null;
        if (!EsDestinoValido(_destinoSelId)) return null;
        return _rutas.FirstOrDefault(x => x.Tipo == ModoTipo && Conecta(x, _destinoSelId));
    }

    private Route? RutaDeOpcion(string? opcion)
    {
        if (opcion == null) return null;
        foreach (var r in Destinos)
        {
            string otro = TravelService.OtroExtremo(r, _state.CurrentLocationId);
            var l = _lugares.FirstOrDefault(x => x.Id == otro);
            if (l == null) continue;
            if (opcion.StartsWith(l.Name)) return r;
        }
        return null;
    }

    private void OnPropertyChanged(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}