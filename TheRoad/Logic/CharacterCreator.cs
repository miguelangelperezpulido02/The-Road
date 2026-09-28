using System;
using System.IO;
using TheRoad.Models;

namespace TheRoad.Logic;

// Reglas de creación de personaje. C# puro, sin WPF.
// Así la UI solo muestra datos y este clase decide qué es válido.
public static class CharacterCreator
{
    public const int MinStat = 1;
    public const int MaxStat = 5;
    public const int PuntosIniciales = 8;
    public const int MaxNombre = 20;

    // Nombres de stats para no repetir strings por la UI.
    public static readonly string[] Stats = ["Fuerza", "Destreza", "Resistencia", "Inteligencia", "Percepcion", "Carisma"];

    public static int GetStat(Player p, string stat) => stat switch
    {
        "Fuerza" => p.Fuerza,
        "Destreza" => p.Destreza,
        "Resistencia" => p.Resistencia,
        "Inteligencia" => p.Inteligencia,
        "Percepcion" => p.Percepcion,
        "Carisma" => p.Carisma,
        _ => MinStat
    };

    public static void SetStat(Player p, string stat, int valor)
    {
        valor = Math.Clamp(valor, MinStat, MaxStat);
        switch (stat)
        {
            case "Fuerza": p.Fuerza = valor; break;
            case "Destreza": p.Destreza = valor; break;
            case "Resistencia": p.Resistencia = valor; break;
            case "Inteligencia": p.Inteligencia = valor; break;
            case "Percepcion": p.Percepcion = valor; break;
            case "Carisma": p.Carisma = valor; break;
        }
    }

    public static int PuntosGastados(Player p)
    {
        return (p.Fuerza - MinStat) + (p.Destreza - MinStat) + (p.Resistencia - MinStat)
             + (p.Inteligencia - MinStat) + (p.Percepcion - MinStat) + (p.Carisma - MinStat);
    }

    public static int PuntosRestantes(Player p) => PuntosIniciales - PuntosGastados(p);

    public static bool PuedeSubir(Player p, string stat)
    {
        return GetStat(p, stat) < MaxStat && PuntosRestantes(p) > 0;
    }

    public static bool PuedeBajar(Player p, string stat)
    {
        return GetStat(p, stat) > MinStat;
    }

    // Null = válido. Texto = motivo del error para mostrar en UI.
    public static string? Validar(Player p)
    {
        if (string.IsNullOrWhiteSpace(p.Name))
            return "Escribe un nombre para tu superviviente.";
        if (p.Name.Trim().Length > MaxNombre)
            return $"El nombre no puede superar {MaxNombre} caracteres.";
        if (PuntosRestantes(p) != 0)
            return $"Reparte todos los puntos. Te quedan {PuntosRestantes(p)}.";
        if (!string.IsNullOrWhiteSpace(p.PhotoPath) && !File.Exists(p.PhotoPath))
            return "La foto seleccionada ya no existe.";
        return null;
    }

    public static Player CrearAleatorio(string nombreBase = "Superviviente")
    {
        var rnd = new Random();
        var p = new Player { Name = nombreBase };
        int restantes = PuntosIniciales;
        while (restantes > 0)
        {
            string stat = Stats[rnd.Next(Stats.Length)];
            if (GetStat(p, stat) < MaxStat)
            {
                SetStat(p, stat, GetStat(p, stat) + 1);
                restantes--;
            }
        }
        return p;
    }

    public static string Descripcion(string stat) => stat switch
    {
        "Fuerza" => "Combate cuerpo a cuerpo y mover obstáculos.",
        "Destreza" => "Huir, sigilo y acciones rápidas.",
        "Resistencia" => "Aguantar hambre, heridas y marchas largas.",
        "Inteligencia" => "Medicina, mecánica y resolver problemas.",
        "Percepcion" => "Detectar peligros y encontrar recursos.",
        "Carisma" => "Persuadir y comerciar con otros supervivientes.",
        _ => string.Empty
    };
}
