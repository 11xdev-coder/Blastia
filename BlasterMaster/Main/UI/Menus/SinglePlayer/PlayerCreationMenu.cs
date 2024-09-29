using BlasterMaster.Main.UI.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI.Menus.SinglePlayer;

public class PlayerCreationMenu : Menu
{
    public PlayerCreationMenu(SpriteFont font, bool isActive = false) : base(font, isActive)
    {
        AddElements();
    }

    private void AddElements()
    {
        Text playerNameText = new Text(Vector2.Zero, "Player Name", Font)
        {
            HAlign = 0.5f,
            VAlign = 0.4f
        };
        Elements.Add(playerNameText);
        
        Input playerNameInput = new Input(Vector2.Zero, Font, true)
        {
            HAlign = 0.5f,
            VAlign = 0.45f
        };
        Elements.Add(playerNameInput);

        Button backButton = new Button(Vector2.Zero, "Back", Font, Back)
        {
            HAlign = 0.5f,
            VAlign = 0.65f
        };
        Elements.Add(backButton);
    }

    private void Back()
    {
        SwitchToMenu(BlasterMasterGame.SinglePlayerMenu);
    }
}