using Blastia.Main.Blocks.Common;
using Blastia.Main.GameState;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Object = Blastia.Main.GameState.Object;

namespace Blastia.Main.UI;

public class RulerHighlight(Vector2 scale = default)
    : Image(Vector2.Zero, BlastiaGame.RulerBlockHighlight, scale), IWorldPositionUi
{
    public override bool Scalable => false;
    public override bool ScalesWithCamera => true;

    public float CachedCameraScale { get; set; }
    public Vector2 WorldPosition { get; set; }
    public Vector2 ScreenPosition { get; set; }

    /// <summary>
    /// Automatically clamps <c>newPosition</c> to block grid
    /// </summary>
    /// <param name="worldPosition">World position</param>
    /// <param name="camera">Camera for converting positions</param>
    public void SetPosition(Vector2 worldPosition, Camera camera)
    {
        // fucking coordiantlknasekjanlfksadhfnsljdafhbnofjkh awer89fhy734298fuirpnfgk,egfn eugfrgunmek gerg
        
        float gridSize = Block.Size;
        // cache for later drawing
        WorldPosition = new Vector2(
            (float)Math.Floor(worldPosition.X / gridSize) * gridSize,
            (float)Math.Floor(worldPosition.Y / gridSize) * gridSize);
        
        // base method
        ((IWorldPositionUi) this).SetPositionBase(camera);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        int scale = MathUtilities.SmoothRound(Block.Size * CachedCameraScale);
        Rectangle destinationRect = new Rectangle(MathUtilities.SmoothRound(ScreenPosition.X), 
            MathUtilities.SmoothRound(ScreenPosition.Y), scale, scale);
        
        spriteBatch.Draw(Texture, destinationRect, DrawColor);
    }
}