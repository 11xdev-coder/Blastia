using BlasterMaster.Main.UI.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI.Menus.SinglePlayer;

public class WorldsMenu : Menu
{
    private ScrollableArea? _worldsList;

    public WorldsMenu(SpriteFont font, bool isActive = false) : base(font, isActive)
    {
        AddElements();
    }

    private void AddElements()
    {
        Viewport worldListViewport = new Viewport(2000, 450);
        _worldsList = new ScrollableArea(Vector2.Zero, worldListViewport) 
        {
            HAlign = 0.5f,
            VAlign = 0.55f
        };
        Elements.Add(_worldsList);

        Button newWorldButton = new Button(Vector2.Zero, "New world", Font, NewWorld)
        {
            HAlign = 0.5f,
            VAlign = 0.85f
        };
        Elements.Add(newWorldButton);

        Button backButton = new Button(Vector2.Zero, "Back", Font, Back)
        {
            HAlign = 0.5f,
            VAlign = 0.9f
        };
        Elements.Add(backButton);
    }

    private void NewWorld() 
    {
    	SwitchToMenu(BlasterMasterGame.WorldCreationMenu);
    }

    public override void OnMenuActive()
    {
        base.OnMenuActive();

        if (_worldsList == null) return;
        _worldsList.ClearChildren();
        
        List<WorldState> worldStates = PlayerManager.Instance.LoadAllWorlds();
        foreach (var state in worldStates)
        {
            // for each loaded world create a new button
            Button worldButton = new Button(Vector2.Zero, state.Name, Font, () => PlayWorld(state));
            _worldsList.AddChild(worldButton);
        }
    }

    private void PlayWorld(WorldState worldState) 
    {
        PlayerManager.Instance.SelectWorld(worldState);
    }

    private void Back()
    {
        SwitchToMenu(BlasterMasterGame.PlayersMenu);
    }
}