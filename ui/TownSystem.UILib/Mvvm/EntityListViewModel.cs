using System.Collections.ObjectModel;
using TownSystem.SharedLib.Core;

namespace TownSystem.UILib.Mvvm;

/// <summary>
/// Готовая ViewModel «список сущностей + выбранная». Почти каждое окно сфер (машины, здания, сотрудники)
/// — это именно она. <see cref="Load"/> сохраняет выделение при обновлении.
/// </summary>
public class EntityListViewModel<T> : ViewModelBase where T : IEntity
{
    private T? _selected;

    /// <summary>
    /// Элементы списка.
    /// </summary>
    public ObservableCollection<T> Items { get; } = new();

    /// <summary>
    /// Выбранный элемент.
    /// </summary>
    public T? SelectedItem { get => _selected; set => SetProperty(ref _selected, value); }

    /// <summary>
    /// Заменяет содержимое списка, не теряя выбранный элемент (ищет по <see cref="IEntity.Id"/>).
    /// </summary>
    public void Load(IEnumerable<T> source)
    {
        var selectedId = SelectedItem?.Id;
        Items.Clear();
        foreach (var item in source) Items.Add(item);
        SelectedItem = selectedId is { } id ? Items.FirstOrDefault(i => i.Id == id) : default;
    }
}
