using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI;

public class PlayerPreview : UIElement
{
    public PlayerPreview(Vector2 position, Vector2 scale = default) : 
        base(position, BlasterMasterGame.PlayerHead, scale)
    {
    }
    
    public override void UpdateBounds()
    {
        if (Texture == null) return;
        
        UpdateBoundsBase(Texture.Width, Texture.Height);
    }
}