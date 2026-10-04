using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using TheRoad.Logic;
using TheRoad.Models;
using TheRoad.ViewModels;

namespace TheRoad.Views;

public partial class MapView : UserControl
{
    private GameViewModel? _vm;
    private bool _isPanning;
    private Point _lastPanPoint;
    private double _currentScale = 1.0;
    private const double MinScale = 0.7;
    private const double MaxScale = 3.0;

    private readonly Dictionary<string, MapNode> _nodeControls = new();
    private readonly Dictionary<string, (Route Ruta, Path Path)> _routePaths = new();

    // Travel animation
    private Storyboard? _travelStoryboard;
    private DoubleAnimation? _vehicleAnimation;
    private Route? _currentTravelRoute;

    public MapView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => OnDataContextChanged();
        Focusable = true;
    }

    public void Refrescar() => DrawMap();

    private void OnDataContextChanged()
    {
        if (_vm != null)
            _vm.PropertyChanged -= Vm_PropertyChanged;

        _vm = DataContext as GameViewModel;

        if (_vm != null)
            _vm.PropertyChanged += Vm_PropertyChanged;

        DrawMap();
    }

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(GameViewModel.DestinoSeleccionadoId)
            or nameof(GameViewModel.OpcionSeleccionada)
            or nameof(GameViewModel.ModoViaje)
            or nameof(GameViewModel.CurrentId))
        {
            UpdateVisualState();
        }
    }

    private void DrawMap()
    {
        var vm = _vm;
        if (vm == null) return;

        RoutesCanvas.Children.Clear();
        NodesCanvas.Children.Clear();
        _nodeControls.Clear();
        _routePaths.Clear();

        string? destino = vm.DestinoSeleccionadoId;
        var xs = vm.Lugares.ToDictionary(l => l.Id, l => l.X);
        double xActual = xs.TryGetValue(vm.CurrentId, out double xa) ? xa : double.MaxValue;
        // Solo avance al oeste: el este queda atrás y no brilla como alcanzable.
        var reachable = new HashSet<string>(vm.Rutas
            .Where(r => r.FromId == vm.CurrentId || r.ToId == vm.CurrentId)
            .Select(r => r.FromId == vm.CurrentId ? r.ToId : r.FromId)
            .Where(id => xs.TryGetValue(id, out double xd) && xd < xActual));

        // Draw routes
        foreach (var r in vm.Rutas)
        {
            var a = vm.Lugares.FirstOrDefault(l => l.Id == r.FromId);
            var b = vm.Lugares.FirstOrDefault(l => l.Id == r.ToId);
            if (a == null || b == null) continue; // Ruta colgada: se omite sin tumbar el mapa.

            bool isSelected = destino != null &&
                ((r.FromId == vm.CurrentId && r.ToId == destino) ||
                 (r.ToId == vm.CurrentId && r.FromId == destino));

            var path = CreateRoutePath(a, b, r, isSelected);
            RoutesCanvas.Children.Add(path);
            _routePaths[$"{r.FromId}-{r.ToId}"] = (r, path);
        }

        // Draw nodes
        int idx = 0;
        foreach (var l in vm.Lugares)
        {
            var node = CreateNode(l, vm, reachable, destino);
            NodesCanvas.Children.Add(node);
            _nodeControls[l.Id] = node;
            // Etiqueta permanente: ciudades abajo, paradas alternando abajo/arriba.
            bool arriba = l.EsParada && idx % 2 == 1;
            NodesCanvas.Children.Add(CreateLabel(l, arriba));
            idx++;
        }

        // Update minimap
        UpdateMinimap(vm);

        // Update info panel
        UpdateInfoPanel(vm, destino);
    }

    private Path CreateRoutePath(Location a, Location b, Route r, bool isSelected)
    {
        var path = new Path();
        var geometry = new LineGeometry(new Point(a.X, a.Y), new Point(b.X, b.Y));
        path.Data = geometry;

        if (r.Tipo == TipoViaje.Largo)
        {
            // Highway: thick solid line with casing
            var casing = new Path
            {
                Data = geometry,
                Stroke = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A)),
                StrokeThickness = isSelected ? 9 : 7,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            RoutesCanvas.Children.Add(casing);

            path.Stroke = isSelected
                ? new SolidColorBrush(Color.FromRgb(0xD8, 0xA5, 0x45))
                : new SolidColorBrush(Color.FromRgb(0x9A, 0x9A, 0x9A));
            path.StrokeThickness = isSelected ? 6 : 4;
        }
        else
        {
            // Secondary: thinner dashed line
            path.Stroke = isSelected
                ? new SolidColorBrush(Color.FromRgb(0xD8, 0xA5, 0x45))
                : new SolidColorBrush(Color.FromRgb(0x5A, 0x5A, 0x4A));
            path.StrokeThickness = isSelected ? 3 : 2;
            path.StrokeDashArray = new DoubleCollection { 8, 6 };
        }

        path.StrokeStartLineCap = PenLineCap.Round;
        path.StrokeEndLineCap = PenLineCap.Round;
        path.Cursor = Cursors.Hand;

        // Hover effect
        path.MouseEnter += (_, _) =>
        {
            if (!isSelected)
            {
                path.Stroke = new SolidColorBrush(Color.FromRgb(0xD8, 0xA5, 0x45));
                path.StrokeThickness += 1;
            }
        };
        path.MouseLeave += (_, _) =>
        {
            if (!isSelected)
            {
                path.Stroke = r.Tipo == TipoViaje.Largo
                    ? new SolidColorBrush(Color.FromRgb(0x9A, 0x9A, 0x9A))
                    : new SolidColorBrush(Color.FromRgb(0x5A, 0x5A, 0x4A));
                path.StrokeThickness = r.Tipo == TipoViaje.Largo ? 4 : 2;
            }
        };

        return path;
    }

    private MapNode CreateNode(Location l, GameViewModel vm, HashSet<string> reachable, string? destino)
    {
        var node = new MapNode
        {
            NodeSize = l.EsParada ? 14 : 22,
            IconSize = l.EsParada ? 8 : 12,
            Icon = GetNodeIcon(l),
            NodeColor = GetNodeColor(l, vm, reachable, destino),
            IsCurrent = l.Id == vm.CurrentId,
            IsVisited = vm.Visitados.Contains(l.Id) && l.Id != vm.CurrentId,
            IsDestination = l.Id == destino,
            IsReachable = reachable.Contains(l.Id) && l.Id != vm.CurrentId && l.Id != destino,
            ToolTipText = $"{l.Name}\n{l.Description}",
            Cursor = Cursors.Hand
        };

        Canvas.SetLeft(node, l.X - node.NodeSize / 2);
        Canvas.SetTop(node, l.Y - node.NodeSize / 2);

        node.MouseLeftButtonDown += (_, _) => vm.SeleccionarDestino(l.Id);

        return node;
    }

    private static string GetNodeIcon(Location l)
    {
        if (l.EsParada) return "◆";
        return l.Id == "dc" ? "★" : "●";
    }

    private static Brush GetNodeColor(Location l, GameViewModel vm, HashSet<string> reachable, string? destino)
    {
        if (l.Id == vm.CurrentId) return new SolidColorBrush(Color.FromRgb(0x6B, 0x8E, 0x5C));
        if (l.Id == destino) return new SolidColorBrush(Color.FromRgb(0xD8, 0xA5, 0x45));
        if (reachable.Contains(l.Id)) return new SolidColorBrush(Color.FromRgb(0xE8, 0xA5, 0x35));
        if (vm.Visitados.Contains(l.Id)) return new SolidColorBrush(Color.FromRgb(0x5A, 0x5A, 0x4A));
        return new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x2A));
    }

    private static TextBlock CreateLabel(Location l, bool arriba)
    {
        var tb = new TextBlock
        {
            Text = l.Name,
            Width = 140,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            FontSize = l.EsParada ? 10 : 13,
            FontWeight = l.EsParada ? FontWeights.Normal : FontWeights.Bold,
            Foreground = l.EsParada
                ? new SolidColorBrush(Color.FromRgb(0xB8, 0xB0, 0xA0))
                : new SolidColorBrush(Color.FromRgb(0xD8, 0xA5, 0x45)),
            Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 4, ShadowDepth = 0, Opacity = 0.9 }
        };
        double half = (l.EsParada ? 14 : 22) / 2.0;
        Canvas.SetLeft(tb, l.X - 70);
        Canvas.SetTop(tb, arriba ? l.Y - half - 18 : l.Y + half + 2);
        return tb;
    }

    private void UpdateVisualState()
    {
        var vm = _vm;
        if (vm == null) return;

        string? destino = vm.DestinoSeleccionadoId;
        var xs = vm.Lugares.ToDictionary(l => l.Id, l => l.X);
        double xActual = xs.TryGetValue(vm.CurrentId, out double xa) ? xa : double.MaxValue;
        var reachable = new HashSet<string>(vm.Rutas
            .Where(r => r.FromId == vm.CurrentId || r.ToId == vm.CurrentId)
            .Select(r => r.FromId == vm.CurrentId ? r.ToId : r.FromId)
            .Where(id => xs.TryGetValue(id, out double xd) && xd < xActual));

        // Update nodes
        foreach (var kvp in _nodeControls)
        {
            var l = vm.Lugares.FirstOrDefault(x => x.Id == kvp.Key);
            if (l == null) continue; // Nodo de un dibujado anterior ya sin lugar: se omite.
            var node = kvp.Value;

            node.IsCurrent = l.Id == vm.CurrentId;
            node.IsVisited = vm.Visitados.Contains(l.Id) && l.Id != vm.CurrentId;
            node.IsDestination = l.Id == destino;
            node.IsReachable = reachable.Contains(l.Id) && l.Id != vm.CurrentId && l.Id != destino;
            node.NodeColor = GetNodeColor(l, vm, reachable, destino);
        }

        // Update routes
        foreach (var kvp in _routePaths)
        {
            var r = kvp.Value.Ruta;

            bool isSelected = destino != null &&
                ((r.FromId == vm.CurrentId && r.ToId == destino) ||
                 (r.ToId == vm.CurrentId && r.FromId == destino));

            var path = kvp.Value.Path;
            if (isSelected)
            {
                path.Stroke = new SolidColorBrush(Color.FromRgb(0xD8, 0xA5, 0x45));
                path.StrokeThickness = r.Tipo == TipoViaje.Largo ? 6 : 3;
            }
            else
            {
                path.Stroke = r.Tipo == TipoViaje.Largo
                    ? new SolidColorBrush(Color.FromRgb(0x9A, 0x9A, 0x9A))
                    : new SolidColorBrush(Color.FromRgb(0x5A, 0x5A, 0x4A));
                path.StrokeThickness = r.Tipo == TipoViaje.Largo ? 4 : 2;
            }
        }

        UpdateMinimap(vm);
        UpdateInfoPanel(vm, destino);
    }

    private void UpdateMinimap(GameViewModel? vm)
    {
        if (vm == null) return;

        MinimapCanvas.Children.Clear();

        double scaleX = MinimapCanvas.Width / MapCanvas.Width;
        double scaleY = MinimapCanvas.Height / MapCanvas.Height;
        double scale = Math.Min(scaleX, scaleY);

        // Draw mini routes
        foreach (var r in vm.Rutas)
        {
            var a = vm.Lugares.FirstOrDefault(l => l.Id == r.FromId);
            var b = vm.Lugares.FirstOrDefault(l => l.Id == r.ToId);
            if (a == null || b == null) continue; // Ruta colgada: se omite.

            var line = new Line
            {
                X1 = a.X * scale,
                Y1 = a.Y * scale,
                X2 = b.X * scale,
                Y2 = b.Y * scale,
                Stroke = r.Tipo == TipoViaje.Largo
                    ? new SolidColorBrush(Color.FromRgb(0x6A, 0x6A, 0x5A))
                    : new SolidColorBrush(Color.FromRgb(0x4A, 0x4A, 0x3A)),
                StrokeThickness = r.Tipo == TipoViaje.Largo ? 1.5 : 0.8,
                StrokeDashArray = r.Tipo == TipoViaje.Largo ? null : new DoubleCollection { 3, 2 },
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            MinimapCanvas.Children.Add(line);
        }

        // Draw mini nodes
        foreach (var l in vm.Lugares)
        {
            var ellipse = new Ellipse
            {
                Width = l.EsParada ? 3 : 5,
                Height = l.EsParada ? 3 : 5,
                Fill = l.Id == vm.CurrentId
                    ? new SolidColorBrush(Color.FromRgb(0x6B, 0x8E, 0x5C))
                    : l.Id == vm.DestinoSeleccionadoId
                        ? new SolidColorBrush(Color.FromRgb(0xD8, 0xA5, 0x45))
                        : vm.Visitados.Contains(l.Id)
                            ? new SolidColorBrush(Color.FromRgb(0x5A, 0x5A, 0x4A))
                            : new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x2A)),
                Stroke = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A)),
                StrokeThickness = 0.5
            };
            Canvas.SetLeft(ellipse, l.X * scale - ellipse.Width / 2);
            Canvas.SetTop(ellipse, l.Y * scale - ellipse.Height / 2);
            MinimapCanvas.Children.Add(ellipse);
        }

        // Update viewport indicator (viewport en coords de contenido / escala, luego a minimapa).
        double vpWidth = MapBorder.ActualWidth / _currentScale * scale;
        double vpHeight = MapBorder.ActualHeight / _currentScale * scale;
        double vpX = -MapTranslateTransform.X / _currentScale * scale;
        double vpY = -MapTranslateTransform.Y / _currentScale * scale;

        MinimapViewport.Width = Math.Max(10, vpWidth);
        MinimapViewport.Height = Math.Max(10, vpHeight);
        Canvas.SetLeft(MinimapViewport, vpX);
        Canvas.SetTop(MinimapViewport, vpY);
    }

    private void UpdateInfoPanel(GameViewModel vm, string? destino)
    {
        if (destino == null)
        {
            InfoPanel.Visibility = Visibility.Collapsed;
            return;
        }

        var l = vm.Lugares.FirstOrDefault(x => x.Id == destino);
        if (l == null)
        {
            InfoPanel.Visibility = Visibility.Collapsed;
            return;
        }

        InfoPanel.Visibility = Visibility.Visible;
        InfoTitle.Text = l.Name;
        InfoRisk.Text = $"Nivel de riesgo: {GetRiskText(vm, destino)}";

        var routeInfo = new List<string>();

        var largo = vm.Rutas.FirstOrDefault(x => x.Tipo == TipoViaje.Largo &&
            ((x.FromId == vm.CurrentId && x.ToId == destino) ||
             (x.ToId == vm.CurrentId && x.FromId == destino)));
        if (largo != null)
        {
            int gas = TravelService.CosteCombustible(largo);
            routeInfo.Add($"🛣 Autopista: {largo.DistanceKm} km · -{gas} gas · {TravelService.TiempoViajeTxt(largo)}");
        }

        var seg = TravelService.CaminoSegmentado(vm.CurrentId, destino, vm.Rutas.ToList(), vm.Lugares.ToList());
        if (seg.Count > 0)
        {
            int km = seg.Sum(x => x.DistanceKm);
            int gas = seg.Sum(TravelService.CosteCombustible);
            string tramos = seg.Count == 1 ? "1 tramo" : $"{seg.Count} tramos";
            routeInfo.Add($"🛤 Secundaria: {tramos} · {km} km · -{gas} gas · {TravelService.TiempoViajeTxt(seg)}");
        }

        InfoRoutes.Text = string.Join("\n", routeInfo);
        InfoDesc.Text = l.Description;
    }

    private static string GetRiskText(GameViewModel vm, string destino)
    {
        var r = vm.Rutas.FirstOrDefault(x =>
            (x.FromId == vm.CurrentId && x.ToId == destino) ||
            (x.ToId == vm.CurrentId && x.FromId == destino));
        return r?.RiesgoTexto ?? "?";
    }

    // ============================================================
    // ZOOM / PAN
    // ============================================================
    private void MapBorder_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Sin ScrollViewer alrededor: la rueda es zoom directo (con o sin Ctrl).
        e.Handled = true;
        ZoomAt(e.GetPosition(MapBorder), e.Delta > 0 ? 1.15 : 1 / 1.15);
    }

    /// <summary>Zoom con ancla en coordenadas del viewport (origen del transform en 0,0).</summary>
    private void ZoomAt(Point ancla, double factor)
    {
        double newScale = Math.Clamp(_currentScale * factor, MinScale, MaxScale);
        if (Math.Abs(newScale - _currentScale) < 0.001) return;

        double ratio = newScale / _currentScale;
        _currentScale = newScale;
        MapScaleTransform.ScaleX = _currentScale;
        MapScaleTransform.ScaleY = _currentScale;

        MapTranslateTransform.X = ancla.X - (ancla.X - MapTranslateTransform.X) * ratio;
        MapTranslateTransform.Y = ancla.Y - (ancla.Y - MapTranslateTransform.Y) * ratio;

        ClampTranslation();
        UpdateMinimap(_vm);
    }

    private Point CentroVista() => new(MapBorder.ActualWidth / 2, MapBorder.ActualHeight / 2);

    private void MapBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ResetView();
            return;
        }

        _isPanning = true;
        _lastPanPoint = e.GetPosition(MapBorder);
        MapBorder.CaptureMouse();
    }

    private void MapBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isPanning = false;
        MapBorder.ReleaseMouseCapture();
    }

    private void MapBorder_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPanning) return;

        Point current = e.GetPosition(MapBorder);
        Vector delta = current - _lastPanPoint;

        MapTranslateTransform.X += delta.X;
        MapTranslateTransform.Y += delta.Y;

        ClampTranslation();
        _lastPanPoint = current;
        UpdateMinimap(_vm);
    }

    private void MapBorder_MouseLeave(object sender, MouseEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            MapBorder.ReleaseMouseCapture();
        }
    }

    private void ClampTranslation()
    {
        double scaledWidth = MapCanvas.Width * _currentScale;
        double scaledHeight = MapCanvas.Height * _currentScale;
        double viewportWidth = MapBorder.ActualWidth;
        double viewportHeight = MapBorder.ActualHeight;

        double minX, maxX, minY, maxY;
        if (scaledWidth <= viewportWidth)
        {
            // Contenido menor que el viewport: centrado, sin paneo al vacío.
            minX = maxX = (viewportWidth - scaledWidth) / 2;
        }
        else
        {
            minX = viewportWidth - scaledWidth;
            maxX = 0;
        }
        if (scaledHeight <= viewportHeight)
        {
            minY = maxY = (viewportHeight - scaledHeight) / 2;
        }
        else
        {
            minY = viewportHeight - scaledHeight;
            maxY = 0;
        }

        MapTranslateTransform.X = Math.Clamp(MapTranslateTransform.X, minX, maxX);
        MapTranslateTransform.Y = Math.Clamp(MapTranslateTransform.Y, minY, maxY);
    }

    private void BtnZoomIn_Click(object sender, RoutedEventArgs e)
    {
        ZoomAt(CentroVista(), 1.25);
    }

    private void BtnZoomOut_Click(object sender, RoutedEventArgs e)
    {
        ZoomAt(CentroVista(), 1 / 1.25);
    }

    private void BtnResetView_Click(object sender, RoutedEventArgs e)
    {
        ResetView();
    }

    private void ResetView()
    {
        _currentScale = 1.0;
        MapScaleTransform.ScaleX = 1.0;
        MapScaleTransform.ScaleY = 1.0;
        CentrarEnActual();
    }

    /// <summary>La vista "casa" centra el nodo actual (no la esquina del canvas).</summary>
    private void CentrarEnActual()
    {
        var vm = _vm;
        var l = vm?.Lugares.FirstOrDefault(x => x.Id == vm.CurrentId);
        double cx = l?.X ?? MapCanvas.Width / 2;
        double cy = l?.Y ?? MapCanvas.Height / 2;
        MapTranslateTransform.X = MapBorder.ActualWidth / 2 - cx * _currentScale;
        MapTranslateTransform.Y = MapBorder.ActualHeight / 2 - cy * _currentScale;
        ClampTranslation();
        UpdateMinimap(_vm);
    }

    // ============================================================
    // TRAVEL ANIMATION
    // ============================================================
    public void StartTravelAnimation(Route route, List<Location> lugares, bool isLongRoute)
    {
        var vm = _vm;
        if (vm == null) return;

        var from = lugares.FirstOrDefault(l => l.Id == (route.FromId == vm.CurrentId ? route.ToId : route.FromId));
        var to = lugares.FirstOrDefault(l => l.Id == (route.FromId == vm.CurrentId ? route.FromId : route.ToId));
        if (from == null || to == null) return; // Extremos desconocidos: sin animación.

        _currentTravelRoute = route;

        // Setup path
        var geometry = new PathGeometry();
        var figure = new PathFigure { StartPoint = new Point(from.X, from.Y) };
        figure.Segments.Add(new LineSegment(new Point(to.X, to.Y), true));
        geometry.Figures.Add(figure);
        TravelPath.Data = geometry;

        TravelPath.Visibility = Visibility.Visible;
        TravelVehicle.Visibility = Visibility.Visible;

        // Animate vehicle along path
        _vehicleAnimation = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromSeconds(isLongRoute ? 2.5 : 1.8),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
        };

        var matrixAnimation = new MatrixAnimationUsingPath
        {
            PathGeometry = geometry,
            Duration = _vehicleAnimation.Duration,
            DoesRotateWithTangent = true
        };

        Storyboard.SetTarget(_vehicleAnimation, TravelVehicle);
        Storyboard.SetTargetProperty(_vehicleAnimation, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(TranslateTransform.X)"));

        _travelStoryboard = new Storyboard();
        _travelStoryboard.Children.Add(_vehicleAnimation);
        _travelStoryboard.Completed += (_, _) =>
        {
            TravelPath.Visibility = Visibility.Collapsed;
            TravelVehicle.Visibility = Visibility.Collapsed;
            _currentTravelRoute = null;
        };

        _travelStoryboard.Begin(this);
    }
}