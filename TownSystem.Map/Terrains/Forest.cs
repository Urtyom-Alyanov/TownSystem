using System;
using System.Collections.Generic;
using System.Text;

namespace TownSystem.Map.Terrains;

public class Forest : Terrain
{
    public override string Name => "Лес";

    public override bool CanBuild => false;

    public override bool IsPassable => true;
}
