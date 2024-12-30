using Blastia.Main.Blocks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI;

public class RulerHighlight : Image, ICameraScalableUI
{
    public override bool Scalable => false;
    public override bool ScalesWithCamera => true;

    public RulerHighlight(Vector2 scale = default) : base(Vector2.Zero, BlastiaGame.RulerBlockHighlight, scale)
    {
        
    }

    /// <summary>
    /// Automatically clamps <c>newPosition</c> to block grid
    /// </summary>
    /// <param name="newPosition"></param>
    public void SetPosition(Vector2 newPosition)
    {
        var newX = Math.Floor(newPosition.X / Block.Size) * Block.Size;
        var newY = Math.Ceiling(newPosition.Y / Block.Size) * Block.Size;
        var pos = new Vector2((float) newX, (float) newY);

        Position = pos;
    }

    public void OnChangedZoom(float newCameraScale)
    {
        Scale = new Vector2(newCameraScale, newCameraScale);
        Console.WriteLine($"New scale: {Scale}");
    }
}