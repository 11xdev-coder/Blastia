using BlasterMaster.Main.UI.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI.Menus.SinglePlayer;

public class WorldsMenu : Menu
{
    public WorldsMenu(SpriteFont font, bool isActive = false) : base(font, isActive)
    {
        AddElements();
    }

    private void AddElements()
    {
        Button backButton = new Button(Vector2.Zero, "Back", Font, Back)
        {
            HAlign = 0.5f,
            VAlign = 0.9f
        };
        Elements.Add(backButton);
    }

    private void Back()
    {
        SwitchToMenu(BlasterMasterGame.SinglePlayerMenu);
    }
}