using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Buttons;

public class ImageButton : Button
{
    public ImageButton(Vector2 position, Texture2D texture, SpriteFont font, Action? onClick) : base(position, "", font, onClick)
    {
        Texture = texture;
    }
    
    public override void UpdateBounds()
    {
        if (Texture == null) return;
        
        UpdateBoundsBase(Texture.Width, Texture.Height);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
        spriteBatch.Draw(Texture, Position, Color.White);
    }
}