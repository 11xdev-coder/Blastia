using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Warnings;

public class AnomalyUi : WarningUiBase
{
    public AnomalyUi(Vector2 position, string text, SpriteFont font) : base(position, text, 
        BlastiaGame.TextureManager.Rescale(BlastiaGame.TextureManager.Get("Anomaly", "UI", "Icons"), new Vector2(3f, 3f)), font, Colors.DepletedYellowAnomaly, Colors.GlowingYellowAnomaly, 1.5f)
    {
    }
}