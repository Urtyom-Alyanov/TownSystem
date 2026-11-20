using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

using TownSystem.Map.UI.ViewModels;

namespace TownSystem.Map.UI.Views;

/// <summary>
/// Сама карта в окне, её можно двигать, приближать и выбирать клетки
/// </summary>
public partial class MapView : UserControl
{
    public const double CellSize = 32;

    private MapViewModel? model;
    private double fitScale = 1;
    private double fillScale = 1;
    private bool showWholeMap;
    private bool showLabels;
    private Pen? reliefPen;
    private MouseButton? dragButton;
    private bool dragging;
    private Point dragStart;
    private Point mapStart;

    // городские объекты ставим по координатам от левого верхнего угла карты
    public Canvas ObjectLayer => MapObjects;

    // Подгружаем разметку самой карты и кнопок
    public MapView()
    {
        InitializeComponent();
    }

    // переключаемся на другую карту и дальше следим за её изменениями
    private void ChangeMap(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (model != null)
            PropertyChangedEventManager.RemoveHandler(model, UpdateMap, string.Empty);

        model = e.NewValue as MapViewModel;
        if (model != null)
            PropertyChangedEventManager.AddHandler(model, UpdateMap, string.Empty);

        LoadMap();
    }

    // смотрим что поменялось, обновляем картинку или выделение
    private void UpdateMap(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MapViewModel.Map):
                LoadMap();
                break;
            case nameof(MapViewModel.SelectedLayer):
            case nameof(MapViewModel.Cells):
                PaintMap();
                break;
            case nameof(MapViewModel.SelectedCell):
                ShowOutline(SelectionOutline, model?.SelectedCell);
                if (showLabels && model?.SelectedLayer == "Ресурсы")
                    PaintResourceLabels();
                break;
        }
    }

    // При загрузке новой карты выставляем её размер и положение
    private void LoadMap()
    {
        MapObjects.Children.Clear();
        MapSurface.Width = model?.Width * CellSize ?? 0;
        MapSurface.Height = model?.Height * CellSize ?? 0;
        showWholeMap = false;
        CenterMap();
        PaintMap();
        ShowOutline(SelectionOutline, model?.SelectedCell);
        ShowOutline(HoverOutline, null);
    }

    // перерисовываем фон и то, что нужно показать на выбранном слое
    private void PaintMap()
    {
        TerrainImage.Source = model == null ? null : MapDrawing.DrawTerrain(model);
        if (model?.SelectedLayer == "Рельеф")
        {
            reliefPen = new Pen(new SolidColorBrush(Color.FromArgb(130, 96, 72, 47)), 0.7 / MapScale.ScaleX);
            ContourImage.Source = MapDrawing.DrawContours(model, reliefPen);
        }
        else
        {
            reliefPen = null;
            ContourImage.Source = null;
        }
        PaintResources();
    }

    // обновляем цвета месторождений и их подписи
    private void PaintResources()
    {
        ResourceImage.Source = model?.SelectedLayer == "Ресурсы" ? MapDrawing.DrawResourceDots(model) : null;
        PaintResourceLabels();
    }

    // Подписи при приблежении рисуем только на видимых клетках
    private void PaintResourceLabels()
    {
        if (!showLabels || model?.SelectedLayer != "Ресурсы")
        {
            ResourceLabelImage.Source = null;
            return;
        }

        double size = MapScale.ScaleX * CellSize;
        int left = Math.Clamp((int)Math.Floor(-MapOffset.X / size), 0, model.Width);
        int top = Math.Clamp((int)Math.Floor(-MapOffset.Y / size), 0, model.Height);
        int right = Math.Clamp((int)Math.Ceiling((MapViewport.ActualWidth - MapOffset.X) / size), left, model.Width);
        int bottom = Math.Clamp((int)Math.Ceiling((MapViewport.ActualHeight - MapOffset.Y) / size), top, model.Height);
        Int32Rect visibleCells = new Int32Rect(left, top, right - left, bottom - top);
        ResourceLabelImage.Source = MapDrawing.DrawResourceLabels(model, visibleCells, VisualTreeHelper.GetDpi(this).PixelsPerDip);
    }

    // подгоняем карту когда окно стало больше или меньше
    private void ResizeMap(object sender, SizeChangedEventArgs e)
    {
        CenterMap();
    }

    // по кнопке «Вся карта» показываем территорию целиком
    private void FitMap(object sender, RoutedEventArgs e)
    {
        showWholeMap = true;
        CenterMap();
    }

    // По кнопке «По экрану» заполняем доступное место картой
    private void FillMap(object sender, RoutedEventArgs e)
    {
        showWholeMap = false;
        CenterMap();
    }

    // считаем масштаб и ставим карту посередине
    private void CenterMap()
    {
        if (model == null || MapViewport.ActualWidth <= 0 || MapViewport.ActualHeight <= 0)
            return;

        double widthScale = MapViewport.ActualWidth / MapSurface.Width;
        double heightScale = MapViewport.ActualHeight / MapSurface.Height;
        // маленький масштаб нужен чтоб увидеть всю карту, больший заполняет окно
        fitScale = Math.Min(widthScale, heightScale);
        fillScale = Math.Max(widthScale, heightScale);
        double scale = showWholeMap ? fitScale : fillScale;
        SetScale(scale);
        MoveMap((MapViewport.ActualWidth - MapSurface.Width * scale) / 2,
            (MapViewport.ActualHeight - MapSurface.Height * scale) / 2);
    }

    // колесом меняем масштаб вокруг курсора
    private void ZoomMap(object sender, MouseWheelEventArgs e)
    {
        if (model == null || dragging || e.Delta == 0)
            return;

        ZoomAt(e.GetPosition(MapViewport), e.Delta > 0 ? 1.2 : 1 / 1.2);
        ShowOutline(HoverOutline, CellAt(e.GetPosition(MapSurface)));
        e.Handled = true;
    }

    // Кнопка + приближает от центра окна
    private void ZoomIn(object sender, RoutedEventArgs e)
    {
        ZoomAt(new Point(MapViewport.ActualWidth / 2, MapViewport.ActualHeight / 2), 1.2);
    }

    // кнопка − отдаляет от центра окна
    private void ZoomOut(object sender, RoutedEventArgs e)
    {
        ZoomAt(new Point(MapViewport.ActualWidth / 2, MapViewport.ActualHeight / 2), 1 / 1.2);
    }

    // при смене масштаба держим исходную точку на месте пока позволяет край карты
    private void ZoomAt(Point center, double step)
    {
        if (model == null || dragging)
            return;

        double scale = MapScale.ScaleX;
        double maxScale = Math.Max(fillScale * 5, 40.0 / CellSize);
        double nextScale = Math.Clamp(scale * step, fitScale, maxScale);
        if (nextScale == scale)
            return;

        // если крутим колесо берём курсор, если жмём кнопку берём центр
        double ratio = nextScale / scale;
        double x = center.X - (center.X - MapOffset.X) * ratio;
        double y = center.Y - (center.Y - MapOffset.Y) * ratio;
        SetScale(nextScale);
        MoveMap(x, y);
    }

    // Меняем масштаб, цифру вверху и то как видны контуры и подписи
    private void SetScale(double scale)
    {
        MapScale.ScaleX = scale;
        MapScale.ScaleY = scale;

        // подстраиваем толщину линий, чтоб при зуме они не становились жирными
        if (reliefPen != null)
            reliefPen.Thickness = 0.7 / scale;
        ZoomText.Text = $"{Math.Round(scale / fillScale * 100)} %";

        bool labels = scale * CellSize >= 19;
        if (showLabels == labels)
            return;

        showLabels = labels;
        if (!showLabels)
            ResourceLabelImage.Source = null;
    }

    // сдвигаем карту и следим чтоб она не уехала за края
    private void MoveMap(double x, double y)
    {
        double spareWidth = MapViewport.ActualWidth - MapSurface.Width * MapScale.ScaleX;
        double spareHeight = MapViewport.ActualHeight - MapSurface.Height * MapScale.ScaleY;
        // если по этой оси карта целиком влезает, ставим её по центру
        MapOffset.X = spareWidth >= 0 ? spareWidth / 2 : Math.Clamp(x, spareWidth, 0);
        MapOffset.Y = spareHeight >= 0 ? spareHeight / 2 : Math.Clamp(y, spareHeight, 0);
        if (showLabels && model?.SelectedLayer == "Ресурсы")
            PaintResourceLabels();
    }

    // Запоминаем начало нажатия, дальше по движению поймём клик это или сдвиг
    private void PressMap(object sender, MouseButtonEventArgs e)
    {
        if (dragButton != null || (e.ChangedButton != MouseButton.Left && e.ChangedButton != MouseButton.Right))
            return;

        if (!MapViewport.CaptureMouse())
            return;

        dragButton = e.ChangedButton;
        dragStart = e.GetPosition(MapViewport);
        mapStart = new Point(MapOffset.X, MapOffset.Y);
        e.Handled = true;
    }

    // пока тянем мышью карта едет, в остальное время подсвечиваем клетку под курсором
    private void MovePointer(object sender, MouseEventArgs e)
    {
        Vector distance = e.GetPosition(MapViewport) - dragStart;
        // если мышь сдвинулась совсем чуть чуть, это ещё щелчок
        if (dragButton != null && (Math.Abs(distance.X) >= 4 || Math.Abs(distance.Y) >= 4))
            dragging = true;

        if (dragging)
        {
            MoveMap(mapStart.X + distance.X, mapStart.Y + distance.Y);
            ShowOutline(HoverOutline, null);
        }
        else
        {
            ShowOutline(HoverOutline, CellAt(e.GetPosition(MapSurface)));
        }
    }

    // отпускаем мышь, при простом клике выбираем клетку
    private void ReleaseMap(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != dragButton)
            return;

        if (!dragging && dragButton == MouseButton.Left && MapViewport.IsMouseOver && model != null)
        {
            MapCellViewModel? cell = CellAt(e.GetPosition(MapSurface));
            if (cell != null)
                model.SelectCellCommand.Execute(cell);
        }

        dragButton = null;
        dragging = false;
        MapViewport.ReleaseMouseCapture();
        e.Handled = true;
    }

    // Если мышь больше не захвачена окном, сбрасываем перетаскивание
    private void LoseCapture(object sender, MouseEventArgs e)
    {
        dragButton = null;
        dragging = false;
    }

    // убираем подсветку когда курсор вышел за карту
    private void LeaveMap(object sender, MouseEventArgs e)
    {
        ShowOutline(HoverOutline, null);
    }

    // смотрим на какую клетку попал указатель
    private MapCellViewModel? CellAt(Point point)
    {
        // делим позицию на размер клетки, так узнаём её столбец и строку
        int x = (int)Math.Floor(point.X / CellSize);
        int y = (int)Math.Floor(point.Y / CellSize);
        if (model == null || !model.Map.IsInside(x, y))
            return null;

        return model.Cells[y * model.Width + x];
    }

    // Ставим рамку на нужную клетку, если клетки нет то прячем рамку
    private static void ShowOutline(Rectangle outline, MapCellViewModel? cell)
    {
        if (cell == null)
        {
            outline.Visibility = Visibility.Collapsed;
            return;
        }

        Canvas.SetLeft(outline, cell.X * CellSize);
        Canvas.SetTop(outline, cell.Y * CellSize);
        outline.Visibility = Visibility.Visible;
    }
}
