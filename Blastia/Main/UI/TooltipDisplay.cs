using System.Drawing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI;

public class TooltipDisplay
{
    private SpriteFont _font;
    private Text _hoverText;
    private bool _updatedHoverTextThisFrame;

    public TooltipDisplay(SpriteFont font)
    {
        _font = font;
        
        _hoverText = new Text(Vector2.Zero, "", font)
        {
            Scale = new Vector2(0.8f, 0.8f)
        };
       
    }

    /// <summary>
    /// Updates hover text under the cursor. If at some frame no text was set, it clears.
    /// So this method should be called in <c>Update</c> methods
    /// </summary>
    /// <param name="text"></param>
    public void SetHoverText(string text)
    {
        _hoverText.Text = text;
        _updatedHoverTextThisFrame = true;
    }

    /// <summary>
    /// Called at the beginning of each frame
    /// </summary>
    public void BeginFrame()
    {
        _updatedHoverTextThisFrame = false;
    }
    
    public void Update()
    {
        // if hover text was not set this frame, clear it
        if (!_updatedHoverTextThisFrame)
        {
            _hoverText.Text = "";
        }
        
        if (string.IsNullOrEmpty(_hoverText.Text))
        {
            return;
        }

        var offsetX = 20f; 
        var offsetY = 20f;
        _hoverText.Position = BlastiaGame.CursorPosition + new Vector2(offsetX, offsetY);
        _hoverText.Update();
        var designScreenWidth = VideoManager.Instance.TargetResolution.X;
        var designScreenHeight = VideoManager.Instance.TargetResolution.Y;
        
        var adjustedPosition = _hoverText.Position;

        // x
        if (_hoverText.Bounds.Right > designScreenWidth) 
        {
            adjustedPosition.X = designScreenWidth - _hoverText.Bounds.Width;
        }
        if (adjustedPosition.X < 0) 
        {
            adjustedPosition.X = 0;
        }

        // y
        if (_hoverText.Bounds.Bottom > designScreenHeight)
        {
            adjustedPosition.Y = designScreenHeight - _hoverText.Bounds.Height;
        }
        if (adjustedPosition.Y < 0) 
        {
            adjustedPosition.Y = 0;
        }

        _hoverText.Position = adjustedPosition;
        _hoverText.Update(); 
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!string.IsNullOrEmpty(_hoverText.Text))
        {
            _hoverText.Draw(spriteBatch);
        }
    }
}