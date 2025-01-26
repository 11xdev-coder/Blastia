using Blastia.Main.UI.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.InGame;

public class InGameMenu(SpriteFont font, bool isActive = false) : Menu(font, isActive)
{
    protected override void AddElements()
    {
        Button settingsButton = new Button(Vector2.Zero, "Settings", Font, OpenSettings)
        {
            HAlign = 0.02f,
            VAlign = 0.98f
        };
        Elements.Add(settingsButton);
    }

    private void OpenSettings()
    {
        SwitchToMenu(BlastiaGame.InGameSettingsMenu);
    }
}