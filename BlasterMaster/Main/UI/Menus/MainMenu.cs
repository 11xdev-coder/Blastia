using BlasterMaster.Main.UI.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI.Menus;

public class MainMenu : Menu
{
    public MainMenu(SpriteFont font, bool isActive = true) : base(font, isActive)
    {
        AddElements();
    }

    private void AddElements()
    {
        Button singlePlayerButton = new Button(new Vector2(0, 500), "Single Player",
            Font, OnClickSinglePlayer)
        {
            HAlign = 0.5f
        };
        Elements.Add(singlePlayerButton);

        Button multiplayerButton = new Button(new Vector2(0, 550), "Multiplayer",
            Font, OnClickMultiplayer)
        {
            HAlign = 0.5f
        };
        Elements.Add(multiplayerButton);

        Button settingsButton = new Button(new Vector2(0, 600), "Settings",
            Font, OnClickSettings)
        {
            HAlign = 0.5f
        };
        Elements.Add(settingsButton);
        
        Button exitButton = new Button(new Vector2(0, 650), "Exit",
            Font, OnClickExit)
        {
            HAlign = 0.5f
        };
        Elements.Add(exitButton);
    }

    private void OnClickSinglePlayer()
    {
        
    }

    private void OnClickMultiplayer()
    {
        
    }

    private void OnClickSettings()
    {
        SwitchToMenu(BlasterMasterGame.SettingsMenu);
    }

    private void OnClickExit()
    {
        BlasterMasterGame.RequestExit();
    }
}