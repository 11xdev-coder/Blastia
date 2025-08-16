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
            BlastiaGame.LogoTexture)
        {
            HAlign = 0.5f
        };
        Elements.Add(logoText);

        var testGif = new AnimatedGif(new Vector2(100, 100), "https://cdn.discordapp.com/attachments/731489147956625428/1406257717680799804/shoebill-bird.gif?ex=68a1cf0e&is=68a07d8e&hm=65da7750074d258d430b90c4913d807e411a5befa8bf5034e0782ab70da2b5ad&");
        Elements.Add(testGif);
    }
}