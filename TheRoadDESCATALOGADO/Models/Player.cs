namespace TheRoad.Models;

// Datos del superviviente. Solo datos, sin reglas ni UI.
public class Player
{
    public string Name { get; set; } = string.Empty;

    // Ruta de la foto elegida por el jugador. Null = sin foto.
    public string? PhotoPath { get; set; }

    // Estadísticas. Rango válido: 1-5 (lo valida Logic/CharacterCreator).
    public int Fuerza { get; set; } = 1;
    public int Destreza { get; set; } = 1;
    public int Resistencia { get; set; } = 1;
    public int Inteligencia { get; set; } = 1;
    public int Percepcion { get; set; } = 1;
    public int Carisma { get; set; } = 1;

    // Estado para la pantalla principal. Valores iniciales los pone GameData.
    public int HP { get; set; } = 100;
    public int Comida { get; set; } = 4;
    public int Agua { get; set; } = 4;
    public int Combustible { get; set; } = 20;
    public int Medicina { get; set; } = 1;

    // Inventario simple: lista de nombres. Abajo en la pantalla principal.
    public List<string> Inventario { get; set; } = [];

    public string Resumen()
    {
        return $"{Name} [FUE:{Fuerza} DES:{Destreza} RES:{Resistencia} INT:{Inteligencia} PER:{Percepcion} CAR:{Carisma}]";
    }
}
