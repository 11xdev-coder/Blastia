using Blastia.Main.Blocks.Common;

namespace Blastia.Main.Blocks;

public class WaterLiquid : LiquidBlock
{
    public WaterLiquid() : base(BlockId.Water, "Water", 2f, 0, 0.5f, 1f)
    {
    }

    public override LiquidBlock CreateNewInstance() => new WaterLiquid();
}