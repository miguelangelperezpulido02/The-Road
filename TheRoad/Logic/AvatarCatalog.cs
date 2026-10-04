using System.IO;

namespace TheRoad.Logic;

// Fotos prediseñadas de PJ: viven en Assets/Images/Player/*.jpg y viajan junto al exe.
// Para añadir más basta soltar el JPG en esa carpeta y registrarlo en Presets.
public sealed record AvatarPreset(string Id, string Nombre, string Archivo);

public static class AvatarCatalog
{
    public static readonly IReadOnlyList<AvatarPreset> Presets =
    [
        new("pj1", "Vagabunda", "PJ1.jpg"),
        new("pj2", "Veterano", "PJ2.jpg"),
        new("pj3", "Enmascarada", "PJ3.jpg"),
    ];

    // Ruta absoluta en disco (junto al exe, no dentro del assembly).
    public static string RutaDe(AvatarPreset p) =>
        Path.Combine(AppContext.BaseDirectory, "Assets", "Images", "Player", p.Archivo);

    // Solo las que existen físicamente (si falta un archivo, no se ofrece).
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
