namespace TheRoad.Models;

// Punto del mapa. X,Y en píxeles sobre el Canvas del MapView (700x380).
public class Location
{
    // Placeholder cuando no hay lugares cargados: evita reventar la partida.
    public static readonly Location Unknown = new()
    {
        Id = "",
        Name = "Ubicación desconocida",
        Description = "No hay datos de lugares cargados."
    };

    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }

    // Parada intermedia de la carretera secundaria: sin servicios, nodo pequeño en el mapa.
    public bool EsParada { get; set; }
}
