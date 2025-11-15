using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI;

public class Image : UIElement
{
    private int _frame;
    /// <summary>
    /// Current frame (0-indexed)
    /// </summary>
    public int Frame 
    {
        get => _frame;
        set => _frame = value % 3;        
    }
    
    private int _frameWidth;
    private int _frameHeight;
    private int _frameCount;
    
    public Image(Vector2 position, Texture2D texture, Vector2 scale = default) : base(position, texture, scale)
    {
        
    }
    
    /// <summary>
    /// Framed image constructor, frames must be placed vertically
    /// </summary>
    public Image(Vector2 position, Texture2D texture, int frameWidth, int frameHeight, int frameCount, Vector2 scale = default) : base(position, texture, scale) 
    {
        _frameWidth = frameWidth;
        _frameHeight = frameHeight;
        _frameCount = frameCount;
    }

    public override void UpdateBounds()
    {
        if (Texture == null) return;
        
        if (_frameCount > 0)
            UpdateBoundsBase(_frameWidth, _frameHeight);
        else
            UpdateBoundsBase(Texture.Width, Texture.Height);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (Texture == null) return;
        
        if (_frameCount <= 0) 
        {
            base.Draw(spriteBatch);
            return;
        }
        
        Vector2 origin = new Vector2(Texture.Width * 0.5f, Texture.Height * 0.5f);
        Vector2 position = new Vector2(Bounds.Center.X, 
            Bounds.Center.Y);
        
        var sourceRect = new Rectangle(0, Frame * _frameHeight, _frameWidth, _frameHeight);
        spriteBatch.Draw(Texture, position, sourceRect, DrawColor * Alpha, 
            Rotation, origin, Scale, SpriteEffects.None, 0f);
    }
}