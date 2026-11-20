namespace TownSystem.UILib.Services;

/// <summary>
/// Диалоги для ViewModel без прямой зависимости от WPF.
///
/// Это удобно в тестах подменять
/// </summary>
public interface IDialogService
{
    /// <summary>Информационное сообщение.</summary>
    void ShowInfo(string message, string title = "TownSystem");

    /// <summary>Сообщение об ошибке.</summary>
    void ShowError(string message, string title = "Ошибка");

    /// <summary>Вопрос «Да/Нет».</summary>
    bool Confirm(string message, string title = "Подтверждение");

    /// <summary>Выбор файла для открытия. Фильтр вида "Сохранение|*.json". Возвращает путь или null.</summary>
    string? PickOpenFile(string filter);

    /// <summary>Выбор файла для сохранения. Возвращает путь или null.</summary>
    string? PickSaveFile(string filter, string defaultFileName);
}
