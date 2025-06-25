using Blastia.Main.Blocks.Common;
using Blastia.Main.Entities.Common;
using Blastia.Main.GameState;
using Blastia.Main.Items;
using Blastia.Main.Physics;
using Microsoft.Xna.Framework;

namespace Blastia.Main.Blocks;

public class LavaLiquid() : LiquidBlock(BlockId.Lava, "Lava", 0.8f, ItemId.LavaBucket)
{
    public override int Alpha => 255;

    public override LiquidBlock CreateNewInstance() => new LavaLiquid();

    protected override void OnEntityEnter(Entity entity)
    {
        base.OnEntityEnter(entity);

        entity.TryDamage(20);
    }

    public override Block Clone() => CreateNewInstance();
}