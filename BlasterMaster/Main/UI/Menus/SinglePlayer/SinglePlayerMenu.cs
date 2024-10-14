using BlasterMaster.Main.UI.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI.Menus.SinglePlayer;

public class SinglePlayerMenu : Menu
{
    private ScrollableArea? _playerList;
    
    public SinglePlayerMenu(SpriteFont font, bool isActive = false) : base(font, isActive)
    {
        AddElements();
    }
    
    private void AddElements()
    {
        Viewport playerListViewPort = new Viewport(2000, 500);
        _playerList = new ScrollableArea(Vector2.Zero, playerListViewPort)
        {
            HAlign = 0.5f,
            VAlign = 0.4f
        };
        Elements.Add(_playerList);
        
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

        if (_playerList == null) return;
        _playerList.ClearChildren();
        
        List<PlayerState> playerStates = PlayerManager.Instance.LoadAllPlayerStates();
        foreach (var state in playerStates)
        {
            // for each loaded player create a new button
            Button playerButton = new Button(Vector2.Zero, state.Name, Font, PlayPlayer);
            _playerList.AddChild(playerButton);
        }
    }

    private void PlayPlayer()
    {
        
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