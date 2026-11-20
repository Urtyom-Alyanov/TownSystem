using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using TownSystem.Map.UI.ViewModels;
using TownSystem.Map.UI.Views;

namespace TownSystem.Map.UI;

/// <summary>
/// Рисует фон карты, высотные контуры и обозначения ресурсов
/// </summary>
internal static class MapDrawing
{
    // собираем одно изображение вместо отдельного элемента для каждой клетки
    public static BitmapSource DrawTerrain(MapViewModel model)
    {
        const int bytesPerPixel = 4;
        int stride = model.Width * bytesPerPixel;
        byte[] pixels = new byte[model.Height * stride];

        // в фоновой картинке у каждой клетки свой пиксель
        foreach (MapCellViewModel cell in model.Cells)
        {
            int index = cell.Y * stride + cell.X * bytesPerPixel;
            Color color = cell.FillColor;
            pixels[index] = color.B;
            pixels[index + 1] = color.G;
            pixels[index + 2] = color.R;
            pixels[index + 3] = 255;
        }

        return BitmapSource.Create(model.Width, model.Height, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
    }

    // Рисуем линии между ступенями высоты, так рельеф лучше видно
    public static DrawingImage DrawContours(MapViewModel model, Pen pen)
    {
        double size = MapView.CellSize;
        StreamGeometry lines = new StreamGeometry();
        using (StreamGeometryContext drawing = lines.Open())
        {
            for (int y = 0; y < model.Height; y++)
            {
                for (int x = 0; x < model.Width; x++)
                {
                    int index = y * model.Width + x;
                    int band = model.Cells[index].ReliefBand;
                    if (band < 2)
                        continue;

                    double left = x * size;
                    double top = y * size;
                    double right = left + size;
                    double bottom = top + size;

                    // на суше обводим только границы между разными ступенями
                    if (x + 1 < model.Width && IsContour(band, model.Cells[index + 1].ReliefBand))
                    {
                        drawing.BeginFigure(new Point(right, top), false, false);
                        drawing.LineTo(new Point(right, bottom), true, false);
                    }

                    if (y + 1 < model.Height && IsContour(band, model.Cells[index + model.Width].ReliefBand))
                    {
                        drawing.BeginFigure(new Point(left, bottom), false, false);
                        drawing.LineTo(new Point(right, bottom), true, false);
                    }
                }
            }
        }

        lines.Freeze();
        Rect bounds = new Rect(0, 0, model.Width * size, model.Height * size);
        DrawingGroup image = new DrawingGroup();
        image.ClipGeometry = new RectangleGeometry(bounds);
        image.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(bounds)));
        image.Children.Add(new GeometryDrawing(null, pen, lines));
        return new DrawingImage(image);
    }

    // проверяем нужно ли рисовать контур на границе этих клеток
    private static bool IsContour(int band, int neighborBand)
    {
        return neighborBand >= 2 && neighborBand != band;
    }

    // раскрашиваем клетки с месторождениями, чтоб видно было как они распределены
    public static BitmapSource DrawResourceDots(MapViewModel model)
    {
        int stride = model.Width * 4;
        byte[] pixels = new byte[model.Height * stride];

        // на одну клетку хватает одного пикселя даже на большой карте
        foreach (MapCellViewModel cell in model.Cells)
        {
            if (cell.DisplaySymbol.Length == 0 || cell.ResourceColor is not SolidColorBrush brush)
                continue;

            int index = cell.Y * stride + cell.X * 4;
            pixels[index] = brush.Color.B;
            pixels[index + 1] = brush.Color.G;
            pixels[index + 2] = brush.Color.R;
            pixels[index + 3] = 190;
        }

        return BitmapSource.Create(model.Width, model.Height, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
    }

    // Подписываем ресурсы только там, где сейчас видна карта
    public static ImageSource DrawResourceLabels(MapViewModel model, Int32Rect visibleCells, double pixelsPerDip)
    {
        double size = MapView.CellSize;
        DrawingGroup image = new DrawingGroup();
        Dictionary<string, DrawingImage> labels = new Dictionary<string, DrawingImage>();
        using (DrawingContext drawing = image.Open())
        {
            // прозрачный прямоугольник держит размер слоя даже когда значков мало
            drawing.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, model.Width * size, model.Height * size));
            for (int row = visibleCells.Y; row < visibleCells.Y + visibleCells.Height; row++)
            {
                for (int column = visibleCells.X; column < visibleCells.X + visibleCells.Width; column++)
                {
                    MapCellViewModel cell = model.Cells[row * model.Width + column];
                    string symbol = cell.DisplaySymbol;
                    // если подписать все клетки, будет каша, поэтому оставляем часть значков
                    bool showSymbol = cell.IsSelected || (row % 5 == 0 && column % 5 == 0);
                    if (symbol.Length == 0 || !showSymbol)
                        continue;

                    // один и тот же значок не рисуем заново для каждой клетки
                    if (!labels.TryGetValue(symbol, out DrawingImage? label))
                    {
                        label = DrawLabel(symbol, cell.ResourceColor, pixelsPerDip);
                        labels.Add(symbol, label);
                    }

                    double x = (cell.X + 0.5) * size;
                    double y = (cell.Y + 0.5) * size;
                    drawing.DrawImage(label, new Rect(x - 13, y - 10, 26, 20));
                }
            }
        }

        image.Freeze();
        return new DrawingImage(image);
    }

    // Готовим значок ресурса один раз, потом его можно использовать в разных местах
    private static DrawingImage DrawLabel(string symbol, Brush color, double pixelsPerDip)
    {
        FormattedText text = new FormattedText(symbol, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI Semibold"), 14, color, pixelsPerDip);
        DrawingGroup image = new DrawingGroup();
        using (DrawingContext drawing = image.Open())
        {
            drawing.DrawRoundedRectangle(Brushes.Black, null, new Rect(0, 0, 26, 20), 5, 5);
            drawing.DrawText(text, new Point((26 - text.Width) / 2, (20 - text.Height) / 2));
        }

        DrawingImage label = new DrawingImage(image);
        label.Freeze();
        return label;
    }
}
