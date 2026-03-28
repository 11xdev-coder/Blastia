using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Warnings;

public class WarningUi : WarningUiBase
{
    public WarningUi(Vector2 position, string text, SpriteFont font) : base(position, text, 
        BlastiaGame.TextureManager.Rescale(BlastiaGame.TextureManager.Get("Warning", "UI", "Icons"), new Vector2(3f, 3f)), font, Color.DarkRed, Colors.GlowingRedWarning)
    {
    }
}