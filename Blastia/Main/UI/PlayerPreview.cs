using Blastia.Main.Entities;
using Blastia.Main.Entities.HumanLikeEntities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI;

public class PlayerPreview : UIElement
{
    public readonly Player? PlayerInstance;
    public string Name = "";

    private Text? _nameText;
    
    public PlayerPreview(Vector2 position, SpriteFont font, Vector2 scale = default) : 
        base(position, "", font)
    {
        PlayerInstance = new Player(position, null)
        {
            IsPreview = true
        };

        if (Font == null) return;
        _nameText = new Text(position, "", Font);
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

        if (_nameText == null || Font == null) return;
        Vector2 nameTextSize = Font.MeasureString(_nameText.Text);
        
        _nameText.Position = new Vector2(Bounds.Center.X - nameTextSize.X * 0.5f - width * 0.5f, 
            Bounds.Center.Y - nameTextSize.Y - height);
    }

    public override void OnAlignmentChanged()
    {
        base.OnAlignmentChanged();
        
        if(PlayerInstance == null) return;
        PlayerInstance.Position = new Vector2(Bounds.X, Bounds.Y);
    }

    public override void OnMenuInactive()
    {
        base.OnMenuInactive();

        if (_nameText == null) return;
        _nameText.Text = "";
        _nameText.Update();
    }

    public override void Update()
    {
        base.Update();
        
        if(PlayerInstance == null) return;
        PlayerInstance.Update();

        if (_nameText == null) return;
        _nameText.Text = Name;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);

        if (PlayerInstance == null) return;
        PlayerInstance.Draw(spriteBatch);
    }

    public void AddToElements(List<UIElement> elements)
    {
        elements.Add(this);
        if(_nameText != null) elements.Add(_nameText);
    }
}