namespace TownSystem.Map.Models;

/// <summary>
/// Ввысота клетки в метрах от уровня моря
/// Если высота с минусом, значит клетка ниже уровня моря
/// </summary>
public class Relief
{
    public int Height { get; }

    public Relief(int height)
    {
        Height = height;
    }
}
