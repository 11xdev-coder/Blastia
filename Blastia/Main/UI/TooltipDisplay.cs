using System.Drawing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI;

public class TooltipDisplay
{
    private SpriteFont _font;
    private Text _hoverText;
    private bool _updatedHoverTextThisFrame;

    private Image _tooltipBackground;
    
    public TooltipDisplay(SpriteFont font)
    {
        _font = font;
        
        _hoverText = new Text(Vector2.Zero, "", font)
        {
            Scale = new Vector2(0.8f, 0.8f)
        };
        _tooltipBackground = new Image(Vector2.Zero, BlastiaGame.TooltipBackgroundTexture);
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
        
        var offsetX = 20f; 
        var offsetY = 20f;
        if (!string.IsNullOrEmpty(_hoverText.Text))
        {
            _hoverText.Position = BlastiaGame.CursorPosition + new Vector2(offsetX, offsetY);
            _hoverText.Update();
            _hoverText.Position = ScreenEdgeCheck(_hoverText);
            _hoverText.Update(); 
        }
        
        _tooltipBackground.Position = BlastiaGame.CursorPosition + new Vector2(offsetX, offsetY);
        _tooltipBackground.Update();
        _tooltipBackground.Position = ScreenEdgeCheck(_tooltipBackground);
        _tooltipBackground.Update();
    }

    private Vector2 ScreenEdgeCheck(UIElement element)
    {
        var screenWidth = VideoManager.Instance.TargetResolution.X;
        var screenHeight = VideoManager.Instance.TargetResolution.Y;
        var adjustedPosition = element.Position;

        // x
        if (element.Bounds.Right > screenWidth)
        {
            adjustedPosition.X = screenWidth - element.Bounds.Width;
        }
        if (adjustedPosition.X < 0)
        {
            adjustedPosition.X = 0;
        }

        if (element.Bounds.Bottom > screenHeight)
        {
            adjustedPosition.Y = screenHeight - element.Bounds.Height;
        }
        if (adjustedPosition.Y < 0)
        {
            adjustedPosition.Y = 0;
        }
        
        return adjustedPosition;
    }
    
    public void Draw(SpriteBatch spriteBatch)
    {
        if (!string.IsNullOrEmpty(_hoverText.Text))
        {
            _hoverText.Draw(spriteBatch);
        }
        _tooltipBackground.Draw(spriteBatch);
    }
}