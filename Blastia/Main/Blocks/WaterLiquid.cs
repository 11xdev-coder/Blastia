using Blastia.Main.Blocks.Common;
using Blastia.Main.Items;

namespace Blastia.Main.Blocks;

public class WaterLiquid() : LiquidBlock(BlockId.Water, "Water", 0.2f, ItemId.WaterBucket)
{
    public override LiquidBlock CreateNewInstance() => new WaterLiquid();
    public override Block Clone() => CreateNewInstance();
}