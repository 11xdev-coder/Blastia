using BlasterMaster.Main.UI.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI.Menus;

public class SinglePlayerMenu : Menu
{
    public SinglePlayerMenu(SpriteFont font, bool isActive = false) : base(font, isActive)
    {
        AddElements();
    }

    private void AddElements()
    {
        Input input = new Input(Vector2.Zero, Font, true)
        {
            HAlign = 0.5f,
            VAlign = 0.7f
        };
        Elements.Add(input);
        
        Button newPlayerButton = new Button(Vector2.Zero, "New player", Font, NewPlayer)
        {
            HAlign = 0.5f,
            VAlign = 0.85f
        };
        Elements.Add(newPlayerButton);

        Button backButton = new Button(Vector2.Zero, "Back", Font, Back)
        {
            HAlign = 0.5f,
            VAlign = 0.9f
        };
        Elements.Add(backButton);
    }

    private void NewPlayer()
    {
        PlayerManager.Instance.NewPlayer();
    }

    private void Back()
    {
        SwitchToMenu(BlasterMasterGame.MainMenu);
    }
}