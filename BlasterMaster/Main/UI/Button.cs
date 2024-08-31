using System.Net.Mime;
using BlasterMaster.Main.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BlasterMaster.Main.UI;

public class Button : UIElement
{
    public Color NormalColor = Color.Wheat;
    public Color SelectedColor = Color.Yellow;

    public Button(Vector2 position, string text, SpriteFont font, Action onClick) : 
        base(position, text, font)
    {
        OnClick = onClick;
        
        TextDrawColor = NormalColor;
        
        OnStartHovering = () => { PlayTickSound(); Select(); };
        OnEndHovering = Deselect;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
    }

    private void PlayTickSound()
    {
        SoundEngine.Instance.PlaySound(SoundID.Tick);
    }

    private void Select()
    {
        TextDrawColor = SelectedColor;
    }

    private void Deselect()
    {
        TextDrawColor = NormalColor;
    }
}