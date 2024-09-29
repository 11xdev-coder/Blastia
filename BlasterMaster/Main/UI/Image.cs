using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI;

public class Image : UIElement
{
    public Image(Vector2 position, Texture2D texture, Vector2 scale = default) : base(position, texture, scale)
    {
        
    }

    public override void UpdateBounds()
    {
        if (Texture == null) return;
        
        UpdateBoundsBase(Texture.Width, Texture.Height);
    }
}