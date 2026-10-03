using TheRoad.Models;

namespace TheRoad.Logic;

public static class TravelService
{
    private static readonly Random _rnd = new();

    public static List<Route> DestinosDesde(GameState s, List<Route> todas)
        => todas.Where(r => r.FromId == s.CurrentLocationId || r.ToId == s.CurrentLocationId).ToList();

    public static List<Route> DestinosDesde(GameState s, List<Route> todas, TipoViaje modo)
        => DestinosDesde(s, todas).Where(r => r.Tipo == modo).ToList();

    public static string OtroExtremo(Route r, string desde)
        => r.FromId == desde ? r.ToId : r.FromId;

    public static int CosteCombustible(Route r)
        => r.Tipo == TipoViaje.Largo
            ? Math.Max(2, r.DistanceKm / 25)
            : Math.Max(1, r.DistanceKm / 45);

    /// <summary>Horas base de una ruta por distancia (~50 km/h en autopista, ~35 km/h en secundaria). Mínimos 3/2 h.</summary>
    public static int HorasBase(Route r)
        => r.Tipo == TipoViaje.Largo
            ? Math.Max(3, r.DistanceKm / 50)
            : Math.Max(2, r.DistanceKm / 35);

    /// <summary>Rango estimado de horas de viaje (base + 0-2 h de imprevistos). Para la UI.</summary>
    public static (int Min, int Max) HorasEstimadas(Route r)
        => (HorasBase(r), HorasBase(r) + 2);

    public static string TiempoViajeTxt(Route r)
    {
        var (min, max) = HorasEstimadas(r);
        return $"{min}-{max} h";
    }

    public static string TiempoViajeTxt(List<Route> rutas)
    {
        int min = rutas.Sum(HorasBase);
        return $"{min}-{min + 2 * rutas.Count} h";
    }

    /// <summary>Probabilidad de evento (%) de una ruta. Única fuente de verdad: la consultan el roll y la UI.</summary>
    public static int ProbabilidadEvento(Route r)
        => r.Tipo == TipoViaje.Largo ? 35 + 20 * r.Riesgo : 20 + 15 * r.Riesgo;

    public static List<Route> CaminoSegmentado(string desde, string hasta, List<Route> todas)
    {
        if (desde == hasta) return [];

        var secundarias = todas.Where(r => r.Tipo == TipoViaje.Segmentado).ToList();
        var cola = new Queue<(string Id, List<Route> Camino)>();
        var vistos = new HashSet<string> { desde };
        cola.Enqueue((desde, []));

        while (cola.Count > 0)
        {
            var (id, camino) = cola.Dequeue();
            foreach (var r in secundarias.Where(x => x.FromId == id || x.ToId == id))
            {
                string otro = OtroExtremo(r, id);
                if (!vistos.Add(otro)) continue;
                var nuevo = new List<Route>(camino) { r };
                if (otro == hasta) return nuevo;
                cola.Enqueue((otro, nuevo));
            }
        }
        return [];
    }

    public static string? PuedeViajar(GameState s, Route r)
    {
        if (CosteCombustible(r) > s.Player.Combustible)
            return $"Falta combustible (necesitas {CosteCombustible(r)}). Puedes repostar con un bidón del inventario.";
        string? coche = VehicleService.PuedeViajar(s);
        if (coche != null) return coche;
        if (s.Player.HP <= 0)
            return "Estás muerto. No puedes viajar.";
        return null;
    }

    public static TravelResult Viajar(GameState s, Route r, List<Location> lugares)
    {
        string destinoId = OtroExtremo(r, s.CurrentLocationId);
        var destino = lugares.FirstOrDefault(l => l.Id == destinoId);
        if (destino == null)
        {
            // Destino inexistente: se cancela sin mutar el estado.
            return new TravelResult
            {
                Narrative = "Ese destino no existe en el mapa. Viaje cancelado.",
                DestinationId = destinoId,
                DestinationName = "Desconocido",
                Route = r
            };
        }
        bool esLargo = r.Tipo == TipoViaje.Largo;

        int gas = CosteCombustible(r);
        s.Player.Combustible -= gas;
        s.Player.Comida = Math.Max(0, s.Player.Comida - 1);
        s.Player.Agua = Math.Max(0, s.Player.Agua - 1);

        // Duracion variable: horas base por distancia + 0-2 h de imprevistos.
        int horaSalida = s.Hora;
        int horas = HorasBase(r) + _rnd.Next(0, 3);
        s.AvanzarHoras(horas);
        int diasTranscurridos = (horaSalida + horas) / 24;
        s.CurrentLocationId = destinoId;
        s.Visitados.Add(destinoId);

        // Momento de llegada (el roll ocurre tras avanzar el reloj): icono para diario y narrativa.
        string iconoMomento = s.EsDeNoche ? "🌙" : "☀️";

        // Desgaste del vehículo + posible avería.
        int estadoPrevio = s.Vehiculo;
        s.Vehiculo = Math.Max(0, s.Vehiculo - VehicleService.DesgastePorViaje(r));
        bool averia = VehicleService.HayAveria(estadoPrevio);
        int probAveria = VehicleService.ProbabilidadAveria(estadoPrevio);
        if (averia)
            s.Vehiculo = Math.Max(0, s.Vehiculo - VehicleService.DanoAveria);

        string calzada = esLargo ? "de autopista" : "por carretera secundaria";
        string momento = s.EsDeNoche ? "de noche" : "de día";
        string texto = $"Día {s.Dia}: {r.DistanceKm} km {calzada} hasta {destino.Name} (-{gas} gasolina, {horas} h: {horaSalida:00}:00-{s.Hora:00}:00, {momento}). Riesgo {r.RiesgoTexto}.\n{destino.Description}";

        if (s.Player.Agua == 0)
        {
            int sed = Math.Min(s.Player.HP, ItemsService.SedPorViaje);
            s.Player.HP -= sed;
            texto += $"\nLa sed te pasa factura (-{sed} HP).";
        }

        if (s.Player.Comida == 0)
        {
            int hambre = Math.Min(s.Player.HP, ItemsService.HambrePorViaje);
            s.Player.HP -= hambre;
            texto += $"\nEl hambre aprieta (-{hambre} HP).";
        }

        int danoHambruna = ItemsService.AplicarHambruna(s.Player, diasTranscurridos);
        if (danoHambruna > 0)
            texto += $"\nHambre: {diasTranscurridos} día(s) pasado(s) sin comida (-{danoHambruna} HP por hambruna).";

        var events = new List<string>();

        // Probabilidad con la que se rueda este viaje (se captura antes del roll).
        int probEvento = ProbabilidadEvento(r);
        var decisionesPendientes = new List<DataService.DecisionDto>();

        if (_rnd.Next(1, 101) <= probEvento)
        {
            // Oleada 2: todo acierto es decisión (Largo: pool grande; Segmentado: pool menor).
            var dec = esLargo ? Eventos.DecisionGrande(s) : Eventos.DecisionMenor(s);
            if (dec != null)
            {
                decisionesPendientes.Add(dec);
                events.Add(dec.Text);
                s.AnadirDiario($"🎲{iconoMomento} {dec.Text}");
                texto += "\n🎲" + iconoMomento + " " + dec.Text + "\n[Elige una opción.]";
            }
        }

        // Fase C: parada espontánea solo en secundaria (20%, máx 1/viaje, sobre la marcha).
        if (!esLargo && _rnd.Next(100) < 20)
        {
            var esp = Eventos.DecisionRebusca(s);
            if (esp != null)
            {
                decisionesPendientes.Add(esp);
                events.Add(esp.Text);
                s.AnadirDiario($"🎲{iconoMomento} Sobre la marcha: {esp.Text}");
                texto += "\nSobre la marcha: paras un momento a mirar.\n🎲" + iconoMomento + " " + esp.Text + "\n[Elige una opción.]";
            }
        }

        if (destino.EsParada)
        {
            // Oleada 3: la rebusca también es decisión (puede sumarse a la del viaje).
            var reb = Eventos.DecisionRebusca(s);
            if (reb != null)
            {
                decisionesPendientes.Add(reb);
                events.Add(reb.Text);
                s.AnadirDiario($"🎲{iconoMomento} Parada en {destino.Name}: {reb.Text}");
                texto += $"\nParada en {destino.Name}: descansas y miras a ver qué se puede aprovechar.\n🎲{iconoMomento} {reb.Text}\n[Elige una opción.]";
            }
        }

        texto += averia
            ? $"\n⚠ Avería en ruta: el motor tose y pierdes piezas por el camino (-{VehicleService.DanoAveria} vehículo)."
            : string.Empty;
        texto += $"\nVehículo: {s.Vehiculo}/{VehicleService.MaxEstado}.";

        s.AnadirDiario($"llegada a {destino.Name}{(s.EsDeNoche ? " (de noche)" : "")}.");

        return new TravelResult
        {
            Narrative = texto,
            DestinationId = destinoId,
            DestinationName = destino.Name,
            Events = events,
            IsLongRoute = esLargo,
            FuelCost = gas,
            Route = r,
            HuboAveria = averia,
            ProbAveria = probAveria,
            ProbEvento = probEvento,
            DecisionesPendientes = decisionesPendientes,
            HorasViaje = horas,
            HoraSalida = horaSalida,
            HoraLlegada = s.Hora,
            DiasTranscurridos = diasTranscurridos,
            LlegaDeNoche = s.EsDeNoche
        };
    }

    public class TravelResult
    {
        public string Narrative { get; set; } = "";
        public string DestinationId { get; set; } = "";
        public string DestinationName { get; set; } = "";
        public List<string> Events { get; set; } = new();
        public bool IsLongRoute { get; set; }
        public int FuelCost { get; set; }
        public Route Route { get; set; } = null!;

        /// <summary>Hubo avería en este viaje y probabilidad con la que se tiró al salir.</summary>
        public bool HuboAveria { get; set; }
        public int ProbAveria { get; set; }

        /// <summary>Probabilidad usada en la tirada de evento de este viaje.</summary>
        public int ProbEvento { get; set; }

        /// <summary>Decisiones pendientes de elegir, en orden (viaje y luego parada). Efectos sin aplicar.</summary>
        public List<DataService.DecisionDto> DecisionesPendientes { get; set; } = new();

        /// <summary>Horas reales consumidas en este viaje (base por distancia + imprevisto).</summary>
        public int HorasViaje { get; set; }
        public int HoraSalida { get; set; }
        public int HoraLlegada { get; set; }
        public int DiasTranscurridos { get; set; }
        public bool LlegaDeNoche { get; set; }
    }
}
