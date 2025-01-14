using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Buttons;

public class ImageButton : UIElement
{
    public ImageButton(Vector2 position, Texture2D texture, Action? onClick) : base(position, texture)
    {
        OnClick += onClick;

        OnStartHovering = PlayTickSound;
    }
    
    public override void UpdateBounds()
    {
        if (Texture == null) return;
        
        UpdateBoundsBase(Texture.Width, Texture.Height);
    }
}