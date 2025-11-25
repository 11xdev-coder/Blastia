using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus;

public class LogoMenu(SpriteFont font, bool isActive = true) : Menu(font, isActive)
{
    public override ActivationMethod ActivationType => ActivationMethod.OnlyInMenu;
       
    protected override void AddElements()
    {
        LogoImageElement logoText = new LogoImageElement(new Vector2(0, 100),
            BlastiaGame.TextureManager.Get("Logo5X", "Menu"))
        {
            HAlign = 0.5f
        };
        Elements.Add(logoText);
    }
}