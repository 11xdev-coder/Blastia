using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI;

/// <summary>
/// UIElement that draws a colored background at position and scale
/// </summary>
public class PlainBackground : UIElement
{
    private float _width;
    private float _height;
    private Color _color;
    private Rectangle _borderRect;
    private float _borderThickness;
    private Color _borderColor;
    
    public PlainBackground(Vector2 position, float width, float height, Color color, float borderThickness = 0f, Color borderColor = default) 
        : base(position, BlastiaGame.InvisibleTexture, Vector2.One)
    {
        _width = width;
        _height = height;
        _color = color;
        _borderThickness = borderThickness;
        _borderColor = borderColor;
    }

    public override void UpdateBounds()
    {
        UpdateBoundsBase(_width, _height);
        
        _borderRect = new Rectangle(
            (int) (Bounds.X - _borderThickness), (int) (Bounds.Y - _borderThickness), 
            (int) (Bounds.Width + _borderThickness * 2), (int) (Bounds.Height + _borderThickness * 2));
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (_borderThickness > 0)
        {
            // draw 4 separate edges
            // top
            spriteBatch.Draw(BlastiaGame.WhitePixel, new Rectangle(_borderRect.X, _borderRect.Y, _borderRect.Width, (int)_borderThickness), _borderColor);
            // bottom
            spriteBatch.Draw(BlastiaGame.WhitePixel, new Rectangle(_borderRect.X, (int) (_borderRect.Bottom - _borderThickness), _borderRect.Width, (int)_borderThickness), _borderColor);
            // left
            spriteBatch.Draw(BlastiaGame.WhitePixel, new Rectangle(_borderRect.X, _borderRect.Y, (int) _borderThickness, _borderRect.Height), _borderColor);
            // right
            spriteBatch.Draw(BlastiaGame.WhitePixel, new Rectangle((int) (_borderRect.Right - _borderThickness), _borderRect.Y, (int) _borderThickness, _borderRect.Height), _borderColor);
        }
        spriteBatch.Draw(BlastiaGame.WhitePixel, Bounds, _color);
    }
}