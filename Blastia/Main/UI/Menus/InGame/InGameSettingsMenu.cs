using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.InGame;

public class InGameSettingsMenu : Menu
{
    public InGameSettingsMenu(SpriteFont font, bool isActive = false) : base(font, isActive)
    {
        AddElements();
    }

    private void AddElements()
    {
        var tabs = new TabGroup(new Vector2(100, 100), 15, this, 
            new Tab("Video", BlastiaGame.RulerBlockHighlight, BlastiaGame.PlayersMenu));
    }

    private void OpenVideoSettings()
    {
        Console.WriteLine("Video");
    }
}