using System;
using System.Collections.Generic;
using System.Text;

namespace TownSystem.Map.Terrains;

public class Mountain : Terrain
{
    public override string Name => "Горы";

    public override bool CanBuild => false;

    public override bool IsPassable => false;
}
