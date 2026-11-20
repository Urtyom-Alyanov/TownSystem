using System;
using System.Collections.Generic;
using System.Text;

namespace TownSystem.Map.Terrains;

public class Plain : Terrain
{
    public override string Name => "Равнина";

    public override bool CanBuild => true;

    public override bool IsPassable => true;
}
