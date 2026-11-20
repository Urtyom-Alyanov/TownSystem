using System.Windows;

using TownSystem.SharedLib.Simulation;
using TownSystem.UILib.Mvvm;

namespace TownSystem.UILib.Modules;

/// <summary>
/// Интерфейсная часть модуля: даёт оболочке вкладку. View подбирается через <c>DataTemplate</c> по типу ViewModel
/// (см. <see cref="Templates"/>), поэтому оболочка ничего не знает о конкретных окнах.
/// </summary>
public interface IModuleUi
{
    /// <summary>Заголовок вкладки.</summary>
    string Title { get; }

    /// <summary>Порядок вкладок: меньше — левее.</summary>
    int Order => 0;

    /// <summary>Создаёт ViewModel вкладки для мира.</summary>
    ModuleViewModel CreateViewModel(IWorld world);

    /// <summary>
    /// Словарь ресурсов с <c>DataTemplate DataType="{x:Type MyViewModel}"</c>, связывающий ViewModel и View.
    /// Оболочка добавит его в ресурсы приложения.
    /// </summary>
    ResourceDictionary? Templates => null;
}