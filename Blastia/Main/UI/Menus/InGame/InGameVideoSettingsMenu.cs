using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.InGame;

public class InGameVideoSettingsMenu : Menu
{
    public InGameVideoSettingsMenu(SpriteFont font, bool isActive = false) : base(font, isActive)
    {
        AddElements();
    }

    private void AddElements()
    {
        var text = new Text(new Vector2(200, 200), "test 1", Font);
        Elements.Add(text);
    }
}