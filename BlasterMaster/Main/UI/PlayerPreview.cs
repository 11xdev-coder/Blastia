using BlasterMaster.Main.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI;

public class PlayerPreview : UIElement
{
    public readonly PlayerEntity? PlayerInstance;
    
    public PlayerPreview(Vector2 position, Vector2 scale = default) : 
        base(position, BlasterMasterGame.InvisibleTexture, scale)
    {
        PlayerInstance = new PlayerEntity(position)
        {
            IsPreview = true
        };
    }
    
    public override void UpdateBounds()
    {
        if (PlayerInstance == null) return;
        
        // left arm + body + right arm
        float width = PlayerInstance.LeftArm.Image.Width + 
                      PlayerInstance.Body.Image.Width + 
                      PlayerInstance.RightArm.Image.Width;
        
        // leg + body + head
        float height = PlayerInstance.LeftLeg.Image.Height +
                       PlayerInstance.Body.Image.Height +
                       PlayerInstance.Head.Image.Height;
        
        UpdateBoundsBase(width, height);
    }

    public override void OnAlignmentChanged()
    {
        base.OnAlignmentChanged();
        
        if(PlayerInstance == null) return;
        PlayerInstance.Position = new Vector2(Bounds.X, Bounds.Y);
    }

    public override void Update()
    {
        base.Update();
        
        if(PlayerInstance == null) return;
        PlayerInstance.Update();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);

        if (PlayerInstance == null) return;
        PlayerInstance.Draw(spriteBatch);
    }
}