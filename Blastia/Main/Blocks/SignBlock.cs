using Blastia.Main.Blocks.Common;
using Blastia.Main.Entities.HumanLikeEntities;
using Blastia.Main.GameState;
using Blastia.Main.Items;
using Blastia.Main.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.Blocks;

public class SignBlock : Block
{
    public SignBlock() : base(BlockId.Sign, "Sign", 0f, 1.5f, true, false, false,
        ItemId.SignBlock, 1, 0, [SoundID.Dig1, SoundID.Dig2, SoundID.Dig3])
    {
        
    }

    public override TileLayer GetLayer() => TileLayer.Furniture;

    public override bool OnRightClick(World world, Vector2 position, Player player)
    {
        base.OnRightClick(world, position, player);
        if (BlastiaGame.InGameSignEditMenu == null) return false;
        
        Console.WriteLine($"Editing sign at {position}");
        BlastiaGame.InGameSignEditMenu.SignPosition = position;
        BlastiaGame.InGameSignEditMenu.UpdateText();
        BlastiaGame.InGameSignEditMenu.Active = true;

        return true;
    }

    public override void OnBreak(World? world, Vector2 position, Player? player)
    {
        base.OnBreak(world, position, player);
        var worldState = PlayerNWorldManager.Instance.SelectedWorld;
        if (worldState == null) return;
        
        // clear text
        worldState.SignTexts.Remove(position);
    }

    public override void Draw(SpriteBatch spriteBatch, Rectangle destRectangle, Rectangle sourceRectangle, Vector2 worldPosition)
    {
        base.Draw(spriteBatch, destRectangle, sourceRectangle, worldPosition);
        
        // draw overlay depending on text
        var worldState = PlayerNWorldManager.Instance.SelectedWorld;
        if (worldState == null) return;
    
        worldState.SignTexts.TryGetValue(worldPosition, out var text);
        if (string.IsNullOrEmpty(text)) return;

        var overlayRect = new Rectangle(
            destRectangle.X, 
            destRectangle.Y - (int)(destRectangle.Height * 0.03f), 
            (int)(destRectangle.Width * 0.8f),
            (int)(destRectangle.Height * 0.8f)
        );
        switch (text.Length)
        {
            case > 72:
                spriteBatch.Draw(BlastiaGame.SignWrittenOverlay2Texture, overlayRect, Color.White);
                break;
            case > 0:
                spriteBatch.Draw(BlastiaGame.SignWrittenOverlay1Texture, overlayRect, Color.White);
                break;
        }
    }
}