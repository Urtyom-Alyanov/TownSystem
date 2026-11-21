namespace TownSystem.Map.Models;

/// <summary>
/// Высота суши или дна от уровня моря, у воды ещё есть глубина
/// </summary>
public class Relief
{
    public int Height { get; }

    public int Depth { get; }

    // У обычного водоёма поверхность на нулевой отметке
    public Relief(int height) : this(height, height < 0 ? (int)Math.Min(-(long)height, int.MaxValue) : 0)
    {
    }

    // У реки высота дна и глубина считаются отдельно
    internal Relief(int height, int depth)
    {
        Height = height;
        Depth = depth;
    }
}