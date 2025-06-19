using Blastia.Main.Blocks.Common;
using Blastia.Main.Entities.HumanLikeEntities;
using Blastia.Main.GameState;
using Blastia.Main.Items;
using Blastia.Main.Sounds;
using Microsoft.Xna.Framework;

namespace Blastia.Main.Blocks;

public class SignBlock : Block
{
    public SignBlock() : base(BlockId.Sign, "Sign", 0f, 1.5f, false, false,
        ItemId.SignBlock, 1, 0, [SoundID.Dig1, SoundID.Dig2, SoundID.Dig3])
    {
        
    }

    public override void OnRightClick(World world, Vector2 position, Player player)
    {
        base.OnRightClick(world, position, player);
        
        Console.WriteLine($"Editing sign at {position}");
    }
}