using Blastia.Main.Blocks.Common;

namespace Blastia.Main.Blocks;

[Serializable]
public class WaterLiquid : LiquidBlock
{
    public WaterLiquid() : base(BlockId.Water, "Water", 2f, 6)
    {
    }

    protected override LiquidBlock CreateNewInstance() => new WaterLiquid();
}