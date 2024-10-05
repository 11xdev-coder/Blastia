using BlasterMaster.Main.UI.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI.Menus.SinglePlayer;

public class PlayerCreationMenu : Menu
{
    private PlayerPreview? _playerPreview;
    private Input? _nameInput;
    
    public PlayerCreationMenu(SpriteFont font, bool isActive = false) : base(font, isActive)
    {
        AddElements();
    }

    private void AddElements()
    {
        _playerPreview = new PlayerPreview(Vector2.Zero, Font)
        {
            HAlign = 0.8f,
            VAlign = 0.55f
        };
        _playerPreview.AddToElements(Elements);
        
        Text playerNameText = new Text(Vector2.Zero, "Player Name", Font)
        {
            HAlign = 0.5f,
            VAlign = 0.4f
        };
        Elements.Add(playerNameText);
        
        _nameInput = new Input(Vector2.Zero, Font, true)
        {
            HAlign = 0.5f,
            VAlign = 0.45f
        };
        Elements.Add(_nameInput);

        Button backButton = new Button(Vector2.Zero, "Back", Font, Back)
        {
            HAlign = 0.5f,
            VAlign = 0.65f
        };
        Elements.Add(backButton);
    }

    public override void Update()
    {
        base.Update();

        if (_playerPreview == null || _nameInput?.Text == null) return;
        _playerPreview.Name = _nameInput.StringBuilder.ToString();
    }

    private void Back()
    {
        SwitchToMenu(BlasterMasterGame.SinglePlayerMenu);
    }
}