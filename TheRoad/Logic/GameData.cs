using TheRoad.Models;

namespace TheRoad.Logic;

public static class GameData
{
    private static List<Location>? _locationsCache;
    private static List<Route>? _routesCache;
    private static DataService.EventsData? _eventsCache;

    public static List<Location> Locations()
    {
        if (_locationsCache == null)
            _locationsCache = DataService.LoadLocations();
        return _locationsCache;
    }

    public static List<Route> Routes()
    {
        if (_routesCache == null)
            _routesCache = DataService.LoadRoutes();
        return _routesCache;
    }

    public static DataService.EventsData Events
    {
        get
        {
            if (_eventsCache == null)
                _eventsCache = DataService.LoadEvents();
            return _eventsCache;
        }
    }

    public static GameState NuevaPartida(Player jugador)
    {
        // Salida generosa: colchón para no morir en los primeros viajes (techos: 100/20/20/100/10).
        jugador.HP = 100;
        jugador.Comida = 15;
        jugador.Agua = 15;
        jugador.Combustible = 80;
        jugador.Medicina = 5;
        jugador.Inventario = ["Navaja", "Lata de conservas", "Vendaje", "Bidón de gasolina", "Chatarra"];

        return new GameState
        {
            Player = jugador,
            CurrentLocationId = "dc",
            Visitados = ["dc"],
            Dia = 1,
            Hora = 8,
            Vehiculo = VehicleService.EstadoInicial,
            Diario = [$"Día 1: {jugador.Name} sale de Washington D.C."]
        };
    }

    public static Player JugadorDebug() => new()
    {
        Name = "DEBUG",
        Fuerza = 3, Destreza = 3, Resistencia = 3,
        Inteligencia = 3, Percepcion = 3, Carisma = 2
    };

    public static GameState PartidaDebug() => NuevaPartida(JugadorDebug());
}
