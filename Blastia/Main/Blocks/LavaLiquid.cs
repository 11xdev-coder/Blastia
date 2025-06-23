using Blastia.Main.Blocks.Common;
using Blastia.Main.GameState;
using Blastia.Main.Items;
using Blastia.Main.Physics;
using Microsoft.Xna.Framework;

namespace Blastia.Main.Blocks;

public class LavaLiquid() : LiquidBlock(BlockId.Lava, "Lava", 0.8f, ItemId.LavaBucket)
{
    public override int Alpha => 255;

    public override LiquidBlock CreateNewInstance() => new LavaLiquid();

    public override void Update(World world, Vector2 position)
    {
        base.Update(world, position);
        
        var rect = new Rectangle((int)position.X, (int)position.Y, Size, Size);
        var potentialEntities = Collision.GetPotentialEntitiesInRectangle(rect);
        foreach (var entity in potentialEntities)
        {
            if (entity == null) continue;
            
            if (entity.GetBounds().Intersects(rect))
            {
                
            }
        }
    }
}