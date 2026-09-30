using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TheRoad.Models;

namespace TheRoad.Logic
{
    public static class DataService
    {
        private static readonly string DataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

        public static List<Location> LoadLocations()
        {
            var dto = LoadDto<LocationsDto>("locations.json");
            return dto?.Locations?.Where(l => l != null).Select(l => new Location
            {
                Id = l.Id,
                Name = l.Name,
                Description = l.Description,
                X = l.X,
                Y = l.Y,
                EsParada = l.IsStop
            }).ToList() ?? new List<Location>();
        }

        public static List<Route> LoadRoutes()
        {
            var dto = LoadDto<RoutesDto>("routes.json");
            var rutas = dto?.Routes?.Where(r => r != null).ToList() ?? new List<RouteDto>();
            var resultado = new List<Route>();
            foreach (var r in rutas)
            {
                if (!Enum.TryParse<TipoViaje>(r.Type, ignoreCase: true, out var tipo))
                    throw new InvalidOperationException(
                        $"routes.json: ruta {r.FromId} → {r.ToId} tiene type \"{r.Type}\" desconocido. Valores válidos: {string.Join(", ", Enum.GetNames<TipoViaje>())}.");
                resultado.Add(new Route
                {
                    FromId = r.FromId,
                    ToId = r.ToId,
                    DistanceKm = r.DistanceKm,
                    Riesgo = r.Risk,
                    Tipo = tipo
                });
            }
            return resultado;
        }

        public static EventsData LoadEvents()
        {
            return LoadDto<EventsData>("events.json") ?? new EventsData();
        }

        private static T? LoadDto<T>(string fileName) where T : class
        {
            string path = Path.Combine(DataPath, fileName);
            try
            {
                return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
            }
            catch (Exception ex) when (ex is IOException || ex is JsonException)
            {
                throw new InvalidOperationException(
                    $"No se pudo cargar {fileName} desde \"{path}\": {ex.Message}", ex);
            }
        }

        private class LocationsDto { public List<LocationDto> Locations { get; set; } = new(); }
        private class LocationDto
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public double X { get; set; }
            public double Y { get; set; }
            public bool IsStop { get; set; }
            public string Icon { get; set; } = "";
        }

        private class RoutesDto { public List<RouteDto> Routes { get; set; } = new(); }
        private class RouteDto
        {
            public string FromId { get; set; } = "";
            public string ToId { get; set; } = "";
            public int DistanceKm { get; set; }
            public int Risk { get; set; }
            public string Type { get; set; } = "";
        }

        public class EventsData
        {
            public List<EventDto> BigEvents { get; set; } = new();
            public List<EventDto> MinorEvents { get; set; } = new();
            public List<EventDto> ScavengeEvents { get; set; } = new();
        }

        public class EventDto
        {
            public string Text { get; set; } = "";
            public EventEffects Effects { get; set; } = new();
        }

        public class EventEffects
        {
            public int Hp { get; set; }
            public int Food { get; set; }
            public int Water { get; set; }
            public int Medicine { get; set; }
            public int Days { get; set; }
            public List<string> Inventory { get; set; } = new();
        }
    }
}