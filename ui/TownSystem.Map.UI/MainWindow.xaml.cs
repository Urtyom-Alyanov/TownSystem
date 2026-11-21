using System.Windows;

namespace TownSystem.Map.UI;

/// <summary>
/// окно для запуска и просмотра карты отдельно от общего приложения
/// </summary>
public partial class MainWindow : Window
{
    // подгружаем окно с картой из разметки
    public MainWindow()
    {
        InitializeComponent();
    }
}