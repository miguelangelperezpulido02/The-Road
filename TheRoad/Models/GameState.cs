using TheRoad.Logic;

namespace TheRoad.Models;

// Estado de la partida. Lo mueve TravelService, lo muestra GameViewModel.
public class GameState
{
    public Player Player { get; set; } = new();
    public string CurrentLocationId { get; set; } = "dc";
    public HashSet<string> Visitados { get; set; } = ["dc"];
    public int Dia { get; set; } = 1;

    // Reloj del dia (0-23). Lo avanza el viaje; cruzar la medianoche suma dias.
    public int Hora { get; set; } = 8;

    // Calificacion del momento: dia (06:00-19:59) o noche (20:00-05:59).
    // Base para el futuro sistema de eventos por momento del dia.
    public const int HoraAmanecer = 6;
    public const int HoraOcaso = 20;
    public bool EsDeNoche => Hora >= HoraOcaso || Hora < HoraAmanecer;

    public void AvanzarHoras(int horas)
    {
        if (horas <= 0) return;
        Hora += horas;
        while (Hora >= 24) { Hora -= 24; Dia++; }
    }

    // Estado del vehículo (0-100). A 0 bloquea los viajes. Lo mueve TravelService.
    public int Vehiculo { get; set; } = VehicleService.EstadoInicial;

    // Diario: historial simple para la vista Diario.
    public List<string> Diario { get; set; } = [];

    // Huellas: marcas que dejan algunas decisiones y condicionan otras (Fase B).
    public HashSet<string> Marcas { get; set; } = [];

    /// <summary>Derivada: sin HP no hay acciones (game-over). Sin flag que sincronizar.</summary>
    public bool EstaMuerto => Player.HP <= 0;

    /// <summary>Añade una línea al diario con el prefijo de día/hora actual.</summary>
    public void AnadirDiario(string texto) => Diario.Add($"Día {Dia} {Hora:00}:00: {texto}");
}
