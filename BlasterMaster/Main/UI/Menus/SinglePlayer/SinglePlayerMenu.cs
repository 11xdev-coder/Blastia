using BlasterMaster.Main.UI.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI.Menus.SinglePlayer;

public class SinglePlayerMenu : Menu
{
    public SinglePlayerMenu(SpriteFont font, bool isActive = false) : base(font, isActive)
    {
        AddElements();
    }
    
    private void AddElements()
    {
        Viewport playerListViewPort = new Viewport(2000, 500);
        ScrollableArea playerList = new ScrollableArea(Vector2.Zero, playerListViewPort)
        {
            HAlign = 0.5f,
            VAlign = 0.4f
        };
        Elements.Add(playerList);

        Button test = new Button(Vector2.Zero, "TEST", Font, NewPlayer);
        playerList.AddChild(test);
        Button test2 = new Button(Vector2.Zero, "TEST2", Font, NewPlayer);
        playerList.AddChild(test2);
        
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

    public override void OnMenuActive()
    {
        base.OnMenuActive();

        List<PlayerState> playerStates = PlayerManager.Instance.LoadAllPlayerStates();
        foreach (var state in playerStates)
        {
            Console.WriteLine(state.Name);
        }
    }

    private void NewPlayer()
    {
        SwitchToMenu(BlasterMasterGame.PlayerCreationMenu);
    }

    private void Back()
    {
        SwitchToMenu(BlasterMasterGame.MainMenu);
    }
}