using Blastia.Main.Blocks.Common;
using Blastia.Main.GameState;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Object = Blastia.Main.GameState.Object;

namespace Blastia.Main.UI;

public class RulerHighlight : Image, ICameraScalableUI
{
    public override bool Scalable => false;
    public override bool ScalesWithCamera => true;

    private Vector2 _worldPosition;
    private Vector2 _screenPosition;
    private float _cachedCameraScale;

    public RulerHighlight(Vector2 scale = default) : base(Vector2.Zero, BlastiaGame.RulerBlockHighlight, scale)
    {
        
    }

    /// <summary>
    /// Automatically clamps <c>newPosition</c> to block grid
    /// </summary>
    /// <param name="newPosition">World position</param>
    /// <param name="camera"></param>
    public void SetPosition(Vector2 newPosition, Camera camera)
    {
        // fucking coordiantlknasekjanlfksadhfnsljdafhbnofjkh awer89fhy734298fuirpnfgk,egfn eugfrgunmek gerg
        
        _cachedCameraScale = camera.CameraScale;
        
        float gridSize = Block.Size;
        // cache for later drawing
        _worldPosition = new Vector2(
            (float)Math.Floor(newPosition.X / gridSize) * gridSize,
            (float)Math.Floor(newPosition.Y / gridSize) * gridSize);
        
        _screenPosition = camera.WorldToScreen(_worldPosition);
    }

    private void UpdateScreenPosition(Camera camera)
    {
        _screenPosition = camera.WorldToScreen(_worldPosition);
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
        // TODO: ScalesWithCamera property
        Scale = new Vector2(newCameraScale, newCameraScale);
        _cachedCameraScale = newCameraScale;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        int scale = MathUtilities.SmoothRound(Block.Size * _cachedCameraScale);
        Rectangle destinationRect = new Rectangle(MathUtilities.SmoothRound(_screenPosition.X), 
            MathUtilities.SmoothRound(_screenPosition.Y), scale, scale);
        
        spriteBatch.Draw(Texture, destinationRect, Color.White);
    }
}