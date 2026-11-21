using System.Windows.Media;

using TownSystem.Map.Models;
using TownSystem.Map.Terrains;

namespace TownSystem.Map.UI.ViewModels;

/// <summary>
/// берёт одну клетку и готовит то, что нужно показать на карте и в карточке
/// </summary>
public class MapCellViewModel : ViewModelBase
{
    private string layer = "Местность";
    private bool isSelected;

    public MapCell Cell { get; private set; }
    public int X => Cell.Coordinates.X;
    public int Y => Cell.Coordinates.Y;
    public int Height => Cell.Relief.Height;
    public string ReliefLabel => Cell.Terrain is WaterBody ? "Глубина" : "Высота";
    // В карточке глубину показываем со знаком минус от поверхности воды
    public int ReliefValue => Cell.Terrain is WaterBody ? -Cell.Relief.Depth : Height;
    public string CoordinatesText => $"X {X} · Y {Y}";
    public string TerrainName => Cell.Terrain.Name;
    public bool CanBuild => Cell.CanBuild;

    // Запоминаем клетку, с которой работает интерфейс
    public MapCellViewModel(MapCell cell)
    {
        Cell = cell;
    }

    public bool IsSelected
    {
        get => isSelected;
        set
        {
            isSelected = value;
            OnPropertyChanged();
        }
    }

    public string ResourceName => Cell.Deposit?.Resource switch
    {
        NaturalResource.Iron => "Железо",
        NaturalResource.Copper => "Медь",
        NaturalResource.Oil => "Нефть",
        NaturalResource.Gas => "Газ",
        NaturalResource.Coal => "Уголь",
        NaturalResource.Wood => "Древесина",
        _ => "Отсутствует"
    };

    public double ResourceAmount => Cell.Deposit?.Amount ?? 0;

    public string ResourceSymbol => Cell.Deposit?.Resource switch
    {
        NaturalResource.Iron => "Fe",
        NaturalResource.Copper => "Cu",
        NaturalResource.Oil => "Н",
        NaturalResource.Gas => "Г",
        NaturalResource.Coal => "У",
        NaturalResource.Wood => "Д",
        _ => ""
    };

    public Brush ResourceColor => Cell.Deposit?.Resource switch
    {
        NaturalResource.Iron => Brushes.Salmon,
        NaturalResource.Copper => Brushes.Tan,
        NaturalResource.Oil => Brushes.Plum,
        NaturalResource.Gas => Brushes.MediumTurquoise,
        NaturalResource.Coal => Brushes.LightSlateGray,
        NaturalResource.Wood => Brushes.LightGreen,
        _ => Brushes.Transparent
    };

    // На общем плане древесину показываем цветом клетки, а остальные ресурсы точками
    public string DisplaySymbol
    {
        get
        {
            if (layer != "Ресурсы" || ResourceAmount == 0 || Cell.Deposit?.Resource == NaturalResource.Wood)
                return "";

            return ResourceSymbol;
        }
    }

    public string BuildStatus
    {
        get
        {
            if (Cell.IsOccupied)
                return "Занято";

            if (CanBuild)
                return "Застройка";

            if (Cell.CanPlace(PlacementKind.Extraction))
                return "Добыча";

            if (Cell.CanPlace(PlacementKind.WaterInfrastructure))
                return "Водные объекты";

            return "Наземные объекты";
        }
    }

    private static readonly Color openLandColor = Color.FromRgb(243, 238, 223);
    private static readonly Color forestColor = Color.FromRgb(181, 214, 172);
    private static readonly Color mountainColor = Color.FromRgb(168, 175, 182);
    private static readonly Color waterColor = Color.FromRgb(129, 194, 235);

    private static readonly Color[] reliefColors =
    {
        Color.FromRgb(94, 173, 207),
        Color.FromRgb(163, 216, 233),
        Color.FromRgb(173, 207, 145),
        Color.FromRgb(210, 221, 148),
        Color.FromRgb(243, 225, 156),
        Color.FromRgb(234, 185, 119),
        Color.FromRgb(211, 136, 92)
    };

    // первые два цвета для воды, остальные идут от низкого к высокому
    internal int ReliefBand
    {
        get
        {
            if (Cell.Terrain is WaterBody)
                return Cell.Relief.Depth >= 50 ? 0 : 1;

            return 2 + Math.Clamp(Height / 100, 0, 4);
        }
    }

    public Brush CellColor => new SolidColorBrush(FillColor);

    internal Color FillColor
    {
        get
        {
            if (layer == "Рельеф")
                return reliefColors[ReliefBand];

            if (Cell.Terrain is WaterBody)
                return waterColor;

            if (Cell.Terrain is Mountain)
                return mountainColor;

            if (layer == "Ресурсы")
            {
                bool hasWood = Cell.Deposit?.Resource == NaturalResource.Wood && ResourceAmount > 0;
                return hasWood ? forestColor : openLandColor;
            }

            return Cell.Terrain is Forest ? forestColor : openLandColor;
        }
    }

    // Запоминаем новый слой, чтоб обновились цвет и обозначение ресурса
    public void SetLayer(string newLayer)
    {
        layer = newLayer;
        OnPropertyChanged(nameof(CellColor));
        OnPropertyChanged(nameof(DisplaySymbol));
    }

    // подставляем обновлённую клетку из общей карты
    internal void Refresh(MapCell cell)
    {
        Cell = cell;
        // пустое имя говорит интерфейсу обновить сразу все поля
        OnPropertyChanged(string.Empty);
    }
}