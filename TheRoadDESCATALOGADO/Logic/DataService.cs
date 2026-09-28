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
            var json = File.ReadAllText(Path.Combine(DataPath, "locations.json"));
            var dto = JsonConvert.DeserializeObject<LocationsDto>(json);
            return dto?.Locations?.Select(l => new Location
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
            var json = File.ReadAllText(Path.Combine(DataPath, "routes.json"));
            var dto = JsonConvert.DeserializeObject<RoutesDto>(json);
            return dto?.Routes?.Select(r => new Route
            {
                FromId = r.FromId,
                ToId = r.ToId,
                DistanceKm = r.DistanceKm,
                Riesgo = r.Risk,
                Tipo = Enum.Parse<TipoViaje>(r.Type)
            }).ToList() ?? new List<Route>();
        }

        public static EventsData LoadEvents()
        {
            var json = File.ReadAllText(Path.Combine(DataPath, "events.json"));
            return JsonConvert.DeserializeObject<EventsData>(json) ?? new EventsData();
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