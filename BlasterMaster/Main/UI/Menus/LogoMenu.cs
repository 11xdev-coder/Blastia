using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI.Menus;

public class LogoMenu : Menu
{
    private Texture2D _texture;
    
    public LogoMenu(SpriteFont font, Texture2D texture, bool isActive = true) : base(font, isActive)
    {
        _texture = texture;
        AddElements();
    }

    private void AddElements()
    {
        LogoImageElement logoText = new LogoImageElement(new Vector2(0, 100),
            _texture)
        {
            HAlign = 0.5f
        };
        Elements.Add(logoText);
    }
}