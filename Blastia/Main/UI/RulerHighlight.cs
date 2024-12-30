using Blastia.Main.Blocks.Common;
using Blastia.Main.GameState;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Object = Blastia.Main.GameState.Object;

namespace Blastia.Main.UI;

public class RulerHighlight : Image, ICameraScalableUI
{
    public override bool Scalable => false;
    public override bool ScalesWithCamera => true;

    private Vector2 _worldPosition;

    public RulerHighlight(Vector2 scale = default) : base(Vector2.Zero, BlastiaGame.RulerBlockHighlight, scale)
    {
        
    }

    /// <summary>
    /// Automatically clamps <c>newPosition</c> to block grid
    /// </summary>
    /// <param name="newPosition"></param>
    /// <param name="camera">Camera needed to convert screen position to world position</param>
    public void SetPosition(Vector2 newPosition, Camera camera)
    {
        // fucking coords sjakfhsdkjhfhakj4h4wujhwujok344
        
        // Snap to grid in world coordinates
        _worldPosition = new Vector2(
            (float)Math.Floor(newPosition.X / Block.Size) * Block.Size,
            (float)Math.Floor(newPosition.Y / Block.Size) * Block.Size
        );
        
        // Update screen position
        UpdateScreenPosition(camera);
    }
    
    public void UpdateScreenPosition(Camera camera)
    {
        // Convert world position to screen position
        Position = camera.WorldToScreen(_worldPosition);
    }

    public void OnChangedPosition(Object cameraObj)
    {
        if (cameraObj is Camera camera)
        {
            UpdateScreenPosition(camera);
        }
    }

    public void OnChangedZoom(float newCameraScale)
    {
        Scale = new Vector2(newCameraScale, newCameraScale);
    }
}