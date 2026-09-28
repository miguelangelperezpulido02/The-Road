using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TheRoad.Views
{
    public class MapNode : Control
    {
        static MapNode()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MapNode), new FrameworkPropertyMetadata(typeof(MapNode)));
        }

        public static readonly DependencyProperty NodeSizeProperty =
            DependencyProperty.Register(nameof(NodeSize), typeof(double), typeof(MapNode), new PropertyMetadata(18.0));

        public static readonly DependencyProperty IconSizeProperty =
            DependencyProperty.Register(nameof(IconSize), typeof(double), typeof(MapNode), new PropertyMetadata(10.0));

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(string), typeof(MapNode), new PropertyMetadata("●"));

        public static readonly DependencyProperty NodeColorProperty =
            DependencyProperty.Register(nameof(NodeColor), typeof(Brush), typeof(MapNode), new PropertyMetadata(Brushes.DarkRed));

        public static readonly DependencyProperty IsCurrentProperty =
            DependencyProperty.Register(nameof(IsCurrent), typeof(bool), typeof(MapNode), new PropertyMetadata(false));

        public static readonly DependencyProperty IsVisitedProperty =
            DependencyProperty.Register(nameof(IsVisited), typeof(bool), typeof(MapNode), new PropertyMetadata(false));

        public static readonly DependencyProperty IsDestinationProperty =
            DependencyProperty.Register(nameof(IsDestination), typeof(bool), typeof(MapNode), new PropertyMetadata(false));

        public static readonly DependencyProperty IsReachableProperty =
            DependencyProperty.Register(nameof(IsReachable), typeof(bool), typeof(MapNode), new PropertyMetadata(false));

        public static readonly DependencyProperty ToolTipTextProperty =
            DependencyProperty.Register(nameof(ToolTipText), typeof(string), typeof(MapNode), new PropertyMetadata(""));

        public double NodeSize
        {
            get => (double)GetValue(NodeSizeProperty);
            set => SetValue(NodeSizeProperty, value);
        }

        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        public string Icon
        {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public Brush NodeColor
        {
            get => (Brush)GetValue(NodeColorProperty);
            set => SetValue(NodeColorProperty, value);
        }

        public bool IsCurrent
        {
            get => (bool)GetValue(IsCurrentProperty);
            set => SetValue(IsCurrentProperty, value);
        }

        public bool IsVisited
        {
            get => (bool)GetValue(IsVisitedProperty);
            set => SetValue(IsVisitedProperty, value);
        }

        public bool IsDestination
        {
            get => (bool)GetValue(IsDestinationProperty);
            set => SetValue(IsDestinationProperty, value);
        }

        public bool IsReachable
        {
            get => (bool)GetValue(IsReachableProperty);
            set => SetValue(IsReachableProperty, value);
        }

        public string ToolTipText
        {
            get => (string)GetValue(ToolTipTextProperty);
            set => SetValue(ToolTipTextProperty, value);
        }
    }
}