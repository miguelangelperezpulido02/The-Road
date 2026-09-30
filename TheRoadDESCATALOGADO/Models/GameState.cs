using TheRoad.Logic;

namespace TheRoad.Models;

// Estado de la partida. Lo mueve TravelService, lo muestra GameViewModel.
public class GameState
{
    public Player Player { get; set; } = new();
    public string CurrentLocationId { get; set; } = "dc";
    public HashSet<string> Visitados { get; set; } = ["dc"];
    public int Dia { get; set; } = 1;

    // Estado del vehículo (0-100). A 0 bloquea los viajes. Lo mueve TravelService.
    public int Vehiculo { get; set; } = VehicleService.EstadoInicial;

    // Diario: historial simple para la vista Diario.
    public List<string> Diario { get; set; } = [];
}
