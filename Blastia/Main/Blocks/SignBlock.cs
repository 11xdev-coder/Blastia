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
        if (BlastiaGame.InGameSignEditMenu == null) return;
        
        Console.WriteLine($"Editing sign at {position}");
        BlastiaGame.InGameSignEditMenu.SignPosition = position;
        BlastiaGame.InGameSignEditMenu.UpdateText();
        BlastiaGame.InGameSignEditMenu.Active = true;
    }

    public override void OnBreak(World? world, Vector2 position, Player? player)
    {
        base.OnBreak(world, position, player);
        var worldState = PlayerNWorldManager.Instance.SelectedWorld;
        if (worldState == null) return;
        
        // clear text
        worldState.SignTexts.Remove(position);
    }
}