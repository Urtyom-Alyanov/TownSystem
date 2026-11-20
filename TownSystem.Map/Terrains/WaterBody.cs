using System;
using System.Collections.Generic;
using System.Text;

namespace TownSystem.Map.Terrains;

public class WaterBody : Terrain
{
    public override string Name => "Водоём";

    public override bool CanBuild => false;

    public override bool IsPassable => false;
}
