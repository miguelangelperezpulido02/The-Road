using System.IO;

namespace TheRoad.Logic;

// Avatares prediseñados: viven en Assets/Avatares/*.png y viajan junto al exe.
// Para cambiarlos basta sustituir los PNG manteniendo el nombre de archivo.
public sealed record AvatarPreset(string Id, string Nombre, string Archivo);

public static class AvatarCatalog
{
    public static readonly IReadOnlyList<AvatarPreset> Presets =
    [
        new("viajero", "Viajero", "viajero.png"),
        new("chatarrera", "Chatarrera", "chatarrera.png"),
        new("rastreador", "Rastreador", "rastreador.png"),
        new("doctora", "Doctora", "doctora.png"),
        new("lider", "Líder", "lider.png"),
        new("saqueadora", "Saqueadora", "saqueadora.png"),
    ];

    // Ruta absoluta en disco (junto al exe, no dentro del assembly).
    public static string RutaDe(AvatarPreset p) =>
        Path.Combine(AppContext.BaseDirectory, "Assets", "Avatares", p.Archivo);

    // Solo los que existen físicamente (si el jugador borra uno, no se ofrece).
    public static IEnumerable<(AvatarPreset Preset, string Ruta)> Disponibles()
    {
        foreach (var p in Presets)
        {
            string ruta = RutaDe(p);
            if (File.Exists(ruta))
                yield return (p, ruta);
        }
    }
}
