using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI;

/// <summary>
/// Custom backgrounds with custom images and colors
/// </summary>
public class AdvancedBackground : UIElement
{
    private float _width;
    private float _height;
    private Color _color;
    private Rectangle _borderRect;
    private float _borderThickness;
    private Color _borderColor;
    private Texture2D? _rightBorderImg;
    
    public AdvancedBackground(Vector2 position, float width, float height, Color color, float borderThickness = 0f, Color borderColor = default) 
        : base(position, BlastiaGame.TextureManager.Invisible(), Vector2.One)
    {
        _width = width;
        _height = height;
        _color = color;
        _borderThickness = borderThickness;
        _borderColor = borderColor;
    }
    
    /// <summary>
    /// Sets custom image that will be drawn exactly at right border
    /// </summary>
    public void SetRightBorderImage(Texture2D img) => _rightBorderImg = img;
    
    public new void SetBackgroundColor(Color newColor) => _color = newColor;
    public void SetBorderColor(Color newColor) => _borderColor = newColor;

    public override void UpdateBounds()
    {
        UpdateBoundsBase(_width, _height);
        
        _borderRect = new Rectangle(
            (int) (Bounds.X - _borderThickness), (int) (Bounds.Y - _borderThickness), 
            (int) (Bounds.Width + _borderThickness * 2), (int) (Bounds.Height + _borderThickness * 2));
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var whitePixel = BlastiaGame.TextureManager.WhitePixel();
        if (_borderThickness > 0)
        {
            // draw 4 separate edges
            // top
            spriteBatch.Draw(whitePixel, new Rectangle(_borderRect.X, _borderRect.Y, _borderRect.Width, (int)_borderThickness), _borderColor);
            // bottom
            spriteBatch.Draw(whitePixel, new Rectangle(_borderRect.X, (int) (_borderRect.Bottom - _borderThickness), _borderRect.Width, (int)_borderThickness), _borderColor);
            // left
            spriteBatch.Draw(whitePixel, new Rectangle(_borderRect.X, _borderRect.Y, (int) _borderThickness, _borderRect.Height), _borderColor);
            // right
            spriteBatch.Draw(whitePixel, new Rectangle((int) (_borderRect.Right - _borderThickness), _borderRect.Y, (int) _borderThickness, _borderRect.Height), _borderColor);
        }
        spriteBatch.Draw(whitePixel, Bounds, _color);
        
        
        if (_rightBorderImg != null) 
        {
            spriteBatch.Draw(_rightBorderImg, new Vector2(Bounds.Right, Bounds.Top), Color.White);
        }
    }
}