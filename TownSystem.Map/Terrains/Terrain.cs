using System;
using System.Collections.Generic;
using System.Text;

namespace TownSystem.Map.Terrains;

public abstract class Terrain
{
    public abstract string Name { get; }

    // Возможность строительства
    public abstract bool CanBuild { get; }

    // Возможность передвижения наземного транспорта
    public abstract bool IsPassable { get; }
}
