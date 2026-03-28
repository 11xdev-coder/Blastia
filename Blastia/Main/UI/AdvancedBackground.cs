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
    private Rectangle _outlineRect;
    private float _outlineThickness;
    private Color _outlineColor;
    private Texture2D? _rightBorderImg;
    private Color _borderImgsColor = Color.White;
    
    public AdvancedBackground(Vector2 position, float width, float height, Color color, float outlineThickness = 0f, Color outlineColor = default) 
        : base(position, BlastiaGame.TextureManager.Invisible(), Vector2.One)
    {
        _width = width;
        _height = height;
        _color = color;
        _outlineThickness = outlineThickness;
        _outlineColor = outlineColor;
    }
    
    /// <summary>
    /// Sets custom image that will be drawn exactly at right border. Automatically rescales texture to fit background height
    /// </summary>
    public void SetRightBorderImage(Texture2D img) 
    {
        float factor = _height / img.Height;
        _rightBorderImg = BlastiaGame.TextureManager.Rescale(img, new Vector2(factor, factor));
    }
    
    public void SetBorderImagesColor(Color newColor) => _borderImgsColor = newColor;
    
    public new void SetBackgroundColor(Color newColor) => _color = newColor;
    public void SetOutlineColor(Color newColor) => _outlineColor = newColor;

    public override void UpdateBounds()
    {
        UpdateBoundsBase(_width, _height);
        
        _outlineRect = new Rectangle(
            (int) (Bounds.X - _outlineThickness), (int) (Bounds.Y - _outlineThickness), 
            (int) (Bounds.Width + _outlineThickness * 2), (int) (Bounds.Height + _outlineThickness * 2));
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var whitePixel = BlastiaGame.TextureManager.WhitePixel();
        if (_outlineThickness > 0)
        {
            // draw 4 separate edges
            // top
            spriteBatch.Draw(whitePixel, new Rectangle(_outlineRect.X, _outlineRect.Y, _outlineRect.Width, (int)_outlineThickness), _outlineColor);
            // bottom
            spriteBatch.Draw(whitePixel, new Rectangle(_outlineRect.X, (int) (_outlineRect.Bottom - _outlineThickness), _outlineRect.Width, (int)_outlineThickness), _outlineColor);
            // left
            spriteBatch.Draw(whitePixel, new Rectangle(_outlineRect.X, _outlineRect.Y, (int) _outlineThickness, _outlineRect.Height), _outlineColor);
            // right
            spriteBatch.Draw(whitePixel, new Rectangle((int) (_outlineRect.Right - _outlineThickness), _outlineRect.Y, (int) _outlineThickness, _outlineRect.Height), _outlineColor);
        }
        spriteBatch.Draw(whitePixel, Bounds, _color);
        
        
        if (_rightBorderImg != null) 
        {
            spriteBatch.Draw(_rightBorderImg, new Vector2(Bounds.Right, Bounds.Top), _borderImgsColor);
        }
    }
}