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

    public static int DiasQueConsume(Route r) => 1;

    private static int ProbabilidadEvento(Route r)
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
        s.Dia++;
        s.CurrentLocationId = destinoId;
        s.Visitados.Add(destinoId);

        // Desgaste del vehículo + posible avería.
        int estadoPrevio = s.Vehiculo;
        s.Vehiculo = Math.Max(0, s.Vehiculo - VehicleService.DesgastePorViaje(r));
        bool averia = VehicleService.HayAveria(estadoPrevio);
        if (averia)
            s.Vehiculo = Math.Max(0, s.Vehiculo - VehicleService.DanoAveria);

        string calzada = esLargo ? "de autopista" : "por carretera secundaria";
        string texto = $"Día {s.Dia}: {r.DistanceKm} km {calzada} hasta {destino.Name} (-{gas} gasolina, -1 día). Riesgo {r.RiesgoTexto}.\n{destino.Description}";

        if (s.Player.Comida == 0 || s.Player.Agua == 0)
        {
            s.Player.HP = Math.Max(0, s.Player.HP - 10);
            texto += "\nEl hambre y la sed te pasan factura (-10 HP).";
        }

        var events = new List<string>();

        if (_rnd.Next(1, 101) <= ProbabilidadEvento(r))
        {
            string evt = esLargo ? Eventos.EventoGrande(s) : Eventos.EventoMenor(s);
            if (!string.IsNullOrEmpty(evt)) events.Add(evt);
            texto += "\n" + evt;
        }

        if (destino.EsParada)
        {
            string? chapuceo = Eventos.Chapuceo(s);
            if (chapuceo != null)
            {
                events.Add(chapuceo);
                texto += $"\nParada en {destino.Name}: descansas y miras a ver qué se puede aprovechar.\n{chapuceo}";
            }
        }

        texto += averia
            ? $"\n⚠ Avería en ruta: el motor tose y pierdes piezas por el camino (-{VehicleService.DanoAveria} vehículo)."
            : string.Empty;
        texto += $"\nVehículo: {s.Vehiculo}/{VehicleService.MaxEstado}.";

        s.Diario.Add($"Día {s.Dia}: llegada a {destino.Name}.");

        return new TravelResult
        {
            Narrative = texto,
            DestinationId = destinoId,
            DestinationName = destino.Name,
            Events = events,
            IsLongRoute = esLargo,
            FuelCost = gas,
            Route = r
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
    }
}
