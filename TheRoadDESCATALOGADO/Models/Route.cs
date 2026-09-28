namespace TheRoad.Models;

// Tipo de calzada. Largo = autopista concurrida (ancha, más gasolina, un solo tramo).
// Segmentado = carretera secundaria (fina, se recorre por paradas intermedias).
public enum TipoViaje { Largo, Segmentado }

// Conexión entre dos localizaciones.
public class Route
{
    public string FromId { get; set; } = string.Empty;
    public string ToId { get; set; } = string.Empty;
    public int DistanceKm { get; set; }
    public int Riesgo { get; set; } = 1; // 1=Bajo 2=Medio 3=Alto
    public TipoViaje Tipo { get; set; } = TipoViaje.Largo;

    public string RiesgoTexto => Riesgo switch
    {
        1 => "Bajo",
        2 => "Medio",
        _ => "Alto"
    };

    public string TipoTexto => Tipo == TipoViaje.Largo ? "Autopista" : "Secundaria";
}
