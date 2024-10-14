using BlasterMaster.Main.UI.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI.Menus.SinglePlayer;

public class PlayerCreationMenu : Menu
{
    private PlayerPreview? _playerPreview;
    private Input? _nameInput;
    private Text? _playerExistsText;
    
    public PlayerCreationMenu(SpriteFont font, bool isActive = false) : base(font, isActive)
    {
        AddElements();
    }

    private void AddElements()
    {
        _playerPreview = new PlayerPreview(Vector2.Zero, Font)
        {
            HAlign = 0.7f,
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

        _playerExistsText = new Text(Vector2.Zero, "Player already exists!", Font)
        {
            HAlign = 0.5f,
            VAlign = 0.5f,
            Alpha = 0f,
            DrawColor = BlasterMasterGame.ErrorColor
        };
        Elements.Add(_playerExistsText);
        
        Button createButton = new Button(Vector2.Zero, "Create", Font, CreatePlayer)
        {
            HAlign = 0.5f,
            VAlign = 0.6f
        };
        Elements.Add(createButton);
        
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

        // update color
        if (_playerExistsText == null) return;
        _playerExistsText.DrawColor = BlasterMasterGame.ErrorColor;
    }

    private void CreatePlayer()
    {
        if (_nameInput?.Text == null) return;
        string playerName = _nameInput.StringBuilder.ToString();

        if (!PlayerManager.Instance.PlayerExists(playerName))
        {
            // create player if doesnt exist
            PlayerManager.Instance.NewPlayer(_nameInput.StringBuilder.ToString());
            
            Back(); // go back
        }
        else
        {
            // show text if exists
            if (_playerExistsText == null) return;

            _playerExistsText.Alpha = 1f;
            _playerExistsText.LerpAlphaToZero = true;
        }
    }

    private void Back()
    {
        SwitchToMenu(BlasterMasterGame.SinglePlayerMenu);
    }
}