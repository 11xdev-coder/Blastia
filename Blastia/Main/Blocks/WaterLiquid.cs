using Blastia.Main.Blocks.Common;
using Blastia.Main.Items;

namespace Blastia.Main.Blocks;

public class WaterLiquid : LiquidBlock
{
    public WaterLiquid() : base(BlockId.Water, "Water", 2f, 1, ItemId.WaterBucket)
    {
    }

    public override LiquidBlock CreateNewInstance() => new WaterLiquid();
}