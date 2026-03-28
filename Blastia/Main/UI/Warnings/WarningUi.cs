using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Warnings;

public class WarningUi : WarningUiBase
{
    public WarningUi(Vector2 position, string text, SpriteFont font) : base(position, text, BlastiaGame.TextureManager.Get("Warning", "UI", "Icons"), font)
    {
    }
}