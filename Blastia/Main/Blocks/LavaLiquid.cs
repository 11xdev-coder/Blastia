using Blastia.Main.Blocks.Common;
using Blastia.Main.Items;

namespace Blastia.Main.Blocks;

public class LavaLiquid() : LiquidBlock(BlockId.Lava, "Lava", 0.8f, ItemId.LavaBucket)
{
    public override int Alpha => 255;

    public override LiquidBlock CreateNewInstance() => new LavaLiquid();
}