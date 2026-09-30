using TheRoad.Models;

namespace TheRoad.Logic;

public static class GameData
{
    private static List<Location>? _locationsCache;
    private static List<Route>? _routesCache;

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

    public static DataService.EventsData Events => DataService.LoadEvents();

    public static GameState NuevaPartida(Player jugador)
    {
        jugador.HP = 100;
        jugador.Comida = 4;
        jugador.Agua = 4;
        jugador.Combustible = 20;
        jugador.Medicina = 1;
        jugador.Inventario = ["Navaja", "Lata de conservas", "Vendaje", "Bidón de gasolina", "Chatarra"];

        return new GameState
        {
            Player = jugador,
            CurrentLocationId = "dc",
            Visitados = ["dc"],
            Dia = 1,
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
