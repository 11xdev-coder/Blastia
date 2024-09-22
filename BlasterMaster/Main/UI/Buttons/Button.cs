using System.Net.Mime;
using BlasterMaster.Main.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BlasterMaster.Main.UI.Buttons;

public class Button : UIElement
{
    public Color NormalColor = Color.Wheat;
    public Color SelectedColor = Color.Yellow;

    public Button(Vector2 position, string text, SpriteFont font, Action? onClick, bool a = false) : 
        base(position, text, font)
    {
        OnClick = onClick;
        
        DrawColor = NormalColor;
        
        OnStartHovering = () => { PlayTickSound(); Select(); };
        OnEndHovering = Deselect;
    }

    public override void Update()
    {
        base.Update();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
    }

    private void PlayTickSound()
    {
        SoundEngine.PlaySound(SoundID.Tick);
    }

    private void Select()
    {
        DrawColor = SelectedColor;
    }

    private void Deselect()
    {
        DrawColor = NormalColor;
    }
}