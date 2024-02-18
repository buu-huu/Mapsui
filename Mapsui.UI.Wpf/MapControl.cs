using Mapsui.Extensions;
using Mapsui.UI.Utils;
using Mapsui.UI.Wpf.Extensions;
using Mapsui.Utilities;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Mapsui.UI.Wpf;

public partial class MapControl : Grid, IMapControl, IDisposable
{
    private readonly Rectangle _selectRectangle = CreateSelectRectangle();
    private MPoint? _pointerDownPosition;
    private bool _mouseDown;
    private MPoint? _previousMousePosition;
    private readonly FlingTracker _flingTracker = new();
    private MPoint? _currentMousePosition;

    public MapControl()
    {
        CommonInitialize();
        Initialize();
    }

    private void Initialize()
    {
        _invalidate = () =>
        {
            if (Dispatcher.CheckAccess()) InvalidateCanvas();
            else RunOnUIThread(InvalidateCanvas);
        };

        Children.Add(SkiaCanvas);
        Children.Add(_selectRectangle);

        SkiaCanvas.PaintSurface += SKElementOnPaintSurface;

        // Pointer events
        MouseLeftButtonDown += MapControlMouseLeftButtonDown;
        MouseLeftButtonUp += MapControlMouseLeftButtonUp;
        MouseMove += MapControlMouseMove;
        MouseLeave += MapControlMouseLeave;
        MouseWheel += MapControlMouseWheel;
        TouchUp += MapControlTouchUp;
        ManipulationDelta += OnManipulationDelta;
        ManipulationCompleted += OnManipulationCompleted;
        ManipulationInertiaStarting += OnManipulationInertiaStarting;

        Loaded += MapControlLoaded;

        SizeChanged += MapControlSizeChanged;

        IsManipulationEnabled = true;

        SkiaCanvas.Visibility = Visibility.Visible;
        RefreshGraphics();
    }

    private static Rectangle CreateSelectRectangle()
    {
        return new Rectangle
        {
            Fill = new SolidColorBrush(Colors.Red),
            Stroke = new SolidColorBrush(Colors.Black),
            StrokeThickness = 3,
            RadiusX = 0.5,
            RadiusY = 0.5,
            StrokeDashArray = [3.0],
            Opacity = 0.3,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Left,
            Visibility = Visibility.Collapsed
        };
    }

    private SKElement SkiaCanvas { get; } = CreateSkiaRenderElement();

    private static SKElement CreateSkiaRenderElement()
    {
        return new SKElement
        {
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
    }

    [Obsolete("Use Info and ILayerFeatureInfo", true)]
    public event EventHandler<FeatureInfoEventArgs>? FeatureInfo; // todo: Remove and add sample for alternative

    internal void InvalidateCanvas()
    {
        SkiaCanvas.InvalidateVisual();
    }

    private void MapControlLoaded(object sender, RoutedEventArgs e)
    {
        SetViewportSize();

        Focusable = true;
    }

    private void MapControlMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var mouseWheelDelta = e.Delta;
        _currentMousePosition = e.GetPosition(this).ToMapsui();
        Map.Navigator.MouseWheelZoom(mouseWheelDelta, _currentMousePosition);
    }

    private void MapControlSizeChanged(object sender, SizeChangedEventArgs e)
    {
        Clip = new RectangleGeometry { Rect = new Rect(0, 0, ActualWidth, ActualHeight) };
        SetViewportSize();
    }

    private void MapControlMouseLeave(object sender, MouseEventArgs e)
    {
        _previousMousePosition = null;
        ReleaseMouseCapture();
    }

    private void RunOnUIThread(Action action)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(action);
        }
        else
        {
            action();
        }
    }

    private void MapControlMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _pointerDownPosition = e.GetPosition(this).ToMapsui();

        if (HandleWidgetPointerDown(_pointerDownPosition, true, e.ClickCount, GetShiftPressed()))
            return;

        _previousMousePosition = _pointerDownPosition;
        _mouseDown = true;
        _flingTracker.Clear();
        CaptureMouse();
    }

    private static bool IsInBoxZoomMode()
    {
        return Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
    }

    private void MapControlMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var mousePosition = e.GetPosition(this).ToMapsui();

        if (HandleWidgetPointerUp(mousePosition, _pointerDownPosition, true, e.ClickCount, GetShiftPressed()))
        {
            _mouseDown = false;

            return;
        }

        if (_previousMousePosition != null)
        {
            if (IsInBoxZoomMode())
            {
                var previous = Map.Navigator.Viewport.ScreenToWorld(_previousMousePosition.X, _previousMousePosition.Y);
                var current = Map.Navigator.Viewport.ScreenToWorld(mousePosition.X, mousePosition.Y);
                ZoomToBox(previous, current);
            }
            else if (_pointerDownPosition != null && IsClick(mousePosition, _pointerDownPosition))
            {
                OnInfo(CreateMapInfoEventArgs(mousePosition, _pointerDownPosition, e.ClickCount));
            }
        }

        RefreshData();
        _mouseDown = false;

        double velocityX;
        double velocityY;

        (velocityX, velocityY) = _flingTracker.CalcVelocity(1, DateTime.Now.Ticks);

        if (Math.Abs(velocityX) > 200 || Math.Abs(velocityY) > 200)
        {
            // This was the last finger on screen, so this is a fling
            Map.Navigator.Fling(velocityX, velocityY, 1000);
            e.Handled = true;
        }
        _flingTracker.RemoveId(1);

        _previousMousePosition = new MPoint();
        ReleaseMouseCapture();
    }

    private static bool IsClick(MPoint currentPosition, MPoint? previousPosition)
    {
        if (previousPosition is null)
            return false;

        return
            Math.Abs(currentPosition.X - previousPosition.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(currentPosition.Y - previousPosition.Y) < SystemParameters.MinimumVerticalDragDistance;
    }

    private void MapControlTouchUp(object? sender, TouchEventArgs e)
    {
        var touchPosition = e.GetTouchPoint(this).Position.ToMapsui();
        if (IsClick(touchPosition, _pointerDownPosition))
        {
            OnInfo(CreateMapInfoEventArgs(touchPosition, touchPosition, 1));
        }
    }

    public void OpenBrowser(string url)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            // The default for this has changed in .net core, you have to explicitly set if to true for it to work.
            UseShellExecute = true
        });
    }

    private void MapControlMouseMove(object sender, MouseEventArgs e)
    {
        if (HandleWidgetPointerMove(e.GetPosition(this).ToMapsui(), e.LeftButton == MouseButtonState.Pressed, 0, GetShiftPressed()))
            return;

        if (IsInBoxZoomMode())
        {
            DrawRectangle(e.GetPosition(this));
            return;
        }

        _currentMousePosition = e.GetPosition(this).ToMapsui();

        if (_mouseDown)
        {
            if (_previousMousePosition == null)
            {
                // Usually MapControlMouseLeftButton down initializes _previousMousePosition but in some
                // situations it can be null. So far I could only reproduce this in debug mode when putting
                // a breakpoint and continuing.
                return;
            }

            _flingTracker.AddEvent(1, _currentMousePosition, DateTime.Now.Ticks);
            Map.Navigator.Drag(_currentMousePosition, _previousMousePosition);
            _previousMousePosition = _currentMousePosition;
        }
    }

    public void ZoomToBox(MPoint beginPoint, MPoint endPoint)
    {
        var box = new MRect(beginPoint.X, beginPoint.Y, endPoint.X, endPoint.Y);
        Map.Navigator.ZoomToBox(box, duration: 300); ;
        ClearBBoxDrawing();
    }

    private void ClearBBoxDrawing()
    {
        RunOnUIThread(() => _selectRectangle.Visibility = Visibility.Collapsed);
    }

    private void DrawRectangle(Point newPos)
    {
        if (_mouseDown)
        {
            if (_previousMousePosition == null) return; // can happen during debug

            var from = _previousMousePosition;
            var to = newPos;

            if (from.X > to.X)
            {
                var temp = from;
                from.X = to.X;
                to.X = temp.X;
            }

            if (from.Y > to.Y)
            {
                var temp = from;
                from.Y = to.Y;
                to.Y = temp.Y;
            }

            _selectRectangle.Width = to.X - from.X;
            _selectRectangle.Height = to.Y - from.Y;
            _selectRectangle.Margin = new Thickness(from.X, from.Y, 0, 0);
            _selectRectangle.Visibility = Visibility.Visible;
        }
    }

    private double ViewportWidth => ActualWidth;
    private double ViewportHeight => ActualHeight;

    private static void OnManipulationInertiaStarting(object? sender, ManipulationInertiaStartingEventArgs e)
    {
        e.TranslationBehavior.DesiredDeceleration = 25 * 96.0 / (1000.0 * 1000.0);
    }

    private void OnManipulationDelta(object? sender, ManipulationDeltaEventArgs e)
    {
        Map.Navigator.Pinch(ToTouchManipulation(e));
    }

    private static TouchManipulation ToTouchManipulation(ManipulationDeltaEventArgs e)
    {
        var translation = e.DeltaManipulation.Translation;

        var previousPosition = e.ManipulationOrigin.ToMapsui();
        var position = previousPosition.Offset(translation.X, translation.Y);
        var radius = GetDeltaScale(e.DeltaManipulation.Scale);
        var angle = e.DeltaManipulation.Rotation;

        return new TouchManipulation(position, previousPosition, radius, angle, e.CumulativeManipulation.Rotation);
    }

    private static double GetDeltaScale(Vector scale)
    {
        var deltaScale = (scale.X + scale.Y) / 2;
        if (Math.Abs(deltaScale) < Constants.Epsilon)
            return 1; // If there is no scaling the deltaScale will be 0.0 in Windows Phone (while it is 1.0 in wpf)
        if (!(Math.Abs(deltaScale - 1d) > Constants.Epsilon)) return 1;
        return deltaScale;
    }

    private void OnManipulationCompleted(object? sender, ManipulationCompletedEventArgs e)
    {
        Refresh();
    }

    private void SKElementOnPaintSurface(object? sender, SKPaintSurfaceEventArgs args)
    {
        if (PixelDensity <= 0)
            return;

        var canvas = args.Surface.Canvas;

        canvas.Scale(PixelDensity, PixelDensity);

        CommonDrawControl(canvas);
    }

    private double GetPixelDensity()
    {
        var presentationSource = PresentationSource.FromVisual(this)
            ?? throw new Exception("PresentationSource is null");
        var compositionTarget = presentationSource.CompositionTarget
            ?? throw new Exception("CompositionTarget is null");
        var matrix = compositionTarget.TransformToDevice;

        var dpiX = matrix.M11;
        var dpiY = matrix.M22;

        if (dpiX != dpiY) throw new ArgumentException();

        return dpiX;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _map?.Dispose();
        }

        CommonDispose(disposing);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private static bool GetShiftPressed()
    {
        return Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
    }
}
