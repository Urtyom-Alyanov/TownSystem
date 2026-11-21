using System.Collections.ObjectModel;
using System.Windows.Input;

using TownSystem.Map.Models;
using TownSystem.Map.Services;
using TownSystem.Map.UI.Commands;

using MapModel = TownSystem.Map.Models.Map;

namespace TownSystem.Map.UI.ViewModels;

/// <summary>
/// тут текущая карта, её слой и данные выбранной клетки для нижней панели
/// </summary>
public class MapViewModel : ViewModelBase
{
    private readonly MapGenerator generator = new MapGenerator();
    private string selectedLayer = "Местность";
    private MapCellViewModel? selectedCell;

    public MapModel Map { get; private set; }
    public int Width => Map.Width;
    public int Height => Map.Height;
    public ObservableCollection<MapCellViewModel> Cells { get; } = new ObservableCollection<MapCellViewModel>();
    public string[] Layers { get; } = { "Местность", "Ресурсы", "Рельеф" };
    public bool CanGenerateMap { get; init; } = true;

    public ICommand SelectCellCommand { get; }
    public ICommand GenerateMapCommand { get; }

    // для отдельного запуска сразу создаём свою карту
    public MapViewModel() : this(new MapGenerator().Generate(1600, 800))
    {
    }

    // Принимаем готовую карту из общего приложения чтоб показать именно её
    public MapViewModel(MapModel map)
    {
        Map = map;
        SelectCellCommand = new RelayCommand(SelectCell);
        GenerateMapCommand = new RelayCommand(GenerateMap);
        LoadCells();
    }

    public MapCellViewModel? SelectedCell
    {
        get => selectedCell;
        set
        {
            if (selectedCell != null)
                selectedCell.IsSelected = false;

            selectedCell = value;
            if (selectedCell != null)
                selectedCell.IsSelected = true;

            OnPropertyChanged();
        }
    }

    public string SelectedLayer
    {
        get => selectedLayer;
        set
        {
            if (value == null || selectedLayer == value)
                return;

            selectedLayer = value;
            foreach (MapCellViewModel cell in Cells)
                cell.SetLayer(value);

            OnPropertyChanged();
        }
    }

    // обновляем одну клетку на экране после стройки или добычи
    public void RefreshCell(int x, int y)
    {
        MapCell cell = Map.GetCell(x, y);
        // в списке клетки идут строка за строкой
        Cells[y * Width + x].Refresh(cell);
        OnPropertyChanged(nameof(Cells));
    }

    // Собираем клетки для экрана и убираем старое выделение
    private void LoadCells()
    {
        SelectedCell = null;
        Cells.Clear();
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                MapCellViewModel cell = new MapCellViewModel(Map[x, y]);
                cell.SetLayer(SelectedLayer);
                Cells.Add(cell);
            }
        }
    }

    // выбираем клетку, её данные появятся внизу
    private void SelectCell(object? parameter)
    {
        if (parameter is MapCellViewModel cell)
            SelectedCell = cell;
    }

    // по кнопке заново генерируем карту в тех же размерах
    private void GenerateMap(object? parameter)
    {
        if (!CanGenerateMap)
            return;

        // сначала собираем клетки, а потом говорим интерфейсу что карта готова
        Map = generator.Generate(Width, Height);
        LoadCells();
        OnPropertyChanged(nameof(Map));
    }
}