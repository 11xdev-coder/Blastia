using Blastia.Main.Blocks.Common;

namespace Blastia.Main.Blocks;

[Serializable]
public class WaterLiquid : LiquidBlock
{
    public WaterLiquid() : base(BlockId.Water, "Water", 2f, 8, 0.5f, 1f)
    {
    }

    protected override LiquidBlock CreateNewInstance() => new WaterLiquid();
}