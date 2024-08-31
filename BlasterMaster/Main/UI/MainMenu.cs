using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI;

public class MainMenu : Menu
{
    public MainMenu(SpriteFont font) : base(font)
    {
    }

    protected override void AddElements()
    {
        base.AddElements();

        Text logoText = new Text(new Vector2(70, 50), "Blaster Master",
            Font);
        Elements.Add(logoText);
        
        Button singlePlayerButton = new Button(new Vector2(100, 100), "Single Player",
            Font, OnClickSinglePlayer);
        Elements.Add(singlePlayerButton);

        Button multiplayerButton = new Button(new Vector2(100, 150), "Multiplayer",
            Font, OnClickMultiplayer);
        Elements.Add(multiplayerButton);
    }

    private void OnClickSinglePlayer()
    {
        
    }

    private void OnClickMultiplayer()
    {
        
    }
}