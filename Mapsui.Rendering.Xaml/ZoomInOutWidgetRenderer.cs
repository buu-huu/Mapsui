﻿using Mapsui.Geometries;
using Mapsui.Widgets.ScaleBar;
using Mapsui.Widgets.Zoom;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Mapsui.Rendering.Xaml
{
    public static class ZoomInOutWidgetRenderer
    {
        private const float stroke = 3;

        private static Brush brushStroke;
        private static Brush brushBackground;
        private static Brush brushText;

        public static void Draw(Canvas canvas, ZoomInOutWidget zoomInOut)
        {
            // If this widget belongs to no viewport, than stop drawing
            if (zoomInOut.Viewport == null)
                return;

            brushStroke = new SolidColorBrush(zoomInOut.StrokeColor.ToXaml());
            brushStroke.Opacity = zoomInOut.Opacity;
            brushBackground = new SolidColorBrush(zoomInOut.BackColor.ToXaml());
            brushBackground.Opacity = zoomInOut.Opacity;
            brushText = new SolidColorBrush(zoomInOut.TextColor.ToXaml());
            brushText.Opacity = zoomInOut.Opacity;

            var posX = zoomInOut.CalculatePositionX(0, (float)canvas.Width, zoomInOut.Orientation == Widgets.Zoom.Orientation.Vertical ? zoomInOut.Size : zoomInOut.Size * 2 - stroke);
            var posY = zoomInOut.CalculatePositionY(0, (float)canvas.Height, zoomInOut.Orientation == Widgets.Zoom.Orientation.Vertical ? zoomInOut.Size * 2 - stroke : zoomInOut.Size);

            // Draw a rect for zoom in button
            var rect = new Rectangle();
            rect.Width = zoomInOut.Size;
            rect.Height = zoomInOut.Size;
            rect.Stroke = brushStroke;
            rect.StrokeThickness = stroke;
            rect.Fill = brushBackground;
            rect.RadiusX = 2;
            rect.RadiusY = 2;
            Canvas.SetLeft(rect, posX);
            Canvas.SetTop(rect, posY);
            canvas.Children.Add(rect);

            // Draw a rect for zoom in button
            rect = new Rectangle();
            rect.Width = zoomInOut.Size;
            rect.Height = zoomInOut.Size;
            rect.Stroke = brushStroke;
            rect.StrokeThickness = stroke;
            rect.Fill = brushBackground;
            rect.RadiusX = 2;
            rect.RadiusY = 2;
            Canvas.SetLeft(rect, zoomInOut.Orientation == Widgets.Zoom.Orientation.Vertical ? posX : posX + rect.Width - stroke);
            Canvas.SetTop(rect, zoomInOut.Orientation == Widgets.Zoom.Orientation.Vertical ? posY + rect.Height - stroke : posY);
            canvas.Children.Add(rect);

            // Draw +
            var line = new Line();
            line.X1 = posX + zoomInOut.Size * 0.3;
            line.Y1 = posY + zoomInOut.Size * 0.5;
            line.X2 = posX + zoomInOut.Size * 0.7;
            line.Y2 = posY + zoomInOut.Size * 0.5;
            line.Stroke = brushText;
            line.StrokeThickness = stroke;
            line.StrokeStartLineCap = PenLineCap.Square;
            line.StrokeEndLineCap = PenLineCap.Square;
            canvas.Children.Add(line);

            line = new Line();
            line.X1 = posX + zoomInOut.Size * 0.5;
            line.Y1 = posY + zoomInOut.Size * 0.3;
            line.X2 = posX + zoomInOut.Size * 0.5;
            line.Y2 = posY + zoomInOut.Size * 0.7;
            line.Stroke = brushText;
            line.StrokeThickness = stroke;
            line.StrokeStartLineCap = PenLineCap.Square;
            line.StrokeEndLineCap = PenLineCap.Square;
            canvas.Children.Add(line);

            // Draw -
            line = new Line();
            if (zoomInOut.Orientation == Widgets.Zoom.Orientation.Vertical)
            {
                line.X1 = posX + zoomInOut.Size * 0.3;
                line.Y1 = posY - stroke + zoomInOut.Size * 1.5;
                line.X2 = posX + zoomInOut.Size * 0.7;
                line.Y2 = posY - stroke + zoomInOut.Size * 1.5;
            }
            else
            {
                line.X1 = posX - stroke + zoomInOut.Size * 1.3;
                line.Y1 = posY + zoomInOut.Size * 0.5;
                line.X2 = posX - stroke + zoomInOut.Size * 1.7;
                line.Y2 = posY + zoomInOut.Size * 0.5;
            }
            line.Stroke = brushText;
            line.StrokeThickness = stroke;
            line.StrokeStartLineCap = PenLineCap.Square;
            line.StrokeEndLineCap = PenLineCap.Square;
            canvas.Children.Add(line);

            if (zoomInOut.Orientation == Widgets.Zoom.Orientation.Vertical)
                zoomInOut.Envelope = new BoundingBox(posX, posY, posX + rect.Width, posY + rect.Width * 2 - stroke);
            else
                zoomInOut.Envelope = new BoundingBox(posX, posY, posX + rect.Width * 2 - stroke, posY + rect.Width);
        }
    }
}