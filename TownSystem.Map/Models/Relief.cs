using System;
using System.Collections.Generic;
using System.Text;

namespace TownSystem.Map.Models;

/// <summary>
/// Хранит высоту участка относительно уровня моря
/// </summary>
public class Relief
{
    public int Height { get; }

    public Relief(int height)
    {
        Height = height;
    }
}