using System.Numerics;
using Blastia.Main.Networking;
using Blastia.Main.UI.Buttons;
using Microsoft.Xna.Framework.Graphics;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Blastia.Main.UI.Menus.Multiplayer;

/// <summary>
/// Enter a code to join a game, then shows join status
/// </summary>
/// <param name="font"></param>
/// <param name="isActive"></param>
public class JoinGameMenu(SpriteFont font, bool isActive = false) : Menu(font, isActive)
{
    private Input? _codeEntry;

    private bool _isDrawingStatusText;
    private readonly List<UIElement> _codeEnterElements = [];

    private const string StatusDefaultText = "Searching for lobbies";
    private Text? _statusTextUi;
    private readonly List<UIElement> _statusTextElements = [];
    
    protected override void AddElements()
    {
        _statusTextUi = new Text(Vector2.Zero, "", Font)
        {
            Text = StatusDefaultText,
            HAlign = 0.5f,
            VAlign = 0.5f
        };
        _statusTextElements.Add(_statusTextUi);

        var cancelJoining = new Button(Vector2.Zero, "Cancel", Font, CancelJoin)
        {
            HAlign = 0.5f,
            VAlign = 0.55f
        };
        _statusTextElements.Add(cancelJoining);
        
        _codeEntry = new Input(Vector2.Zero, Font, defaultText: "Enter code...", cursorVisible: true)
        {
            HAlign = 0.5f,
            VAlign = 0.5f,
        };
        _codeEnterElements.Add(_codeEntry);
        
        var joinGame = new Button(Vector2.Zero, "Join", Font, JoinGame)
        {
            HAlign = 0.5f,
            VAlign = 0.55f,
        };
        _codeEnterElements.Add(joinGame);
        
        var back = new Button(Vector2.Zero, "Back", Font, Back)
        {
            HAlign = 0.5f,
            VAlign = 0.6f,
        };
        _codeEnterElements.Add(back);
    }

    public override void Update()
    {
        if (!_isDrawingStatusText || _statusTextUi == null)
            foreach (var codeEnterUi in _codeEnterElements)
                codeEnterUi.Update();
        else
            foreach (var statusTextUi in _statusTextElements)
                statusTextUi.Update();
    }

    public void ToggleStatusText(bool toggle)
    {
        // not showing status text -> back to normal
        if (!toggle && _statusTextUi != null)
            _statusTextUi.Text = StatusDefaultText;
        
        _isDrawingStatusText = toggle;
        Console.WriteLine($"[JoinGameMenu] Drawing status text: {toggle}");
    }

    public void UpdateStatusText(string newText)
    {
        if (_statusTextUi == null) return;
        _statusTextUi.Text = newText;
    }
    
    private void JoinGame()
    {
        if (_codeEntry?.Text == null) return;
        
        ToggleStatusText(true);
        NetworkManager.Instance?.JoinLobbyWithCode(_codeEntry.Text);
    }

    private void CancelJoin()
    {
        // back to entering code and disconnect from lobby
        ToggleStatusText(false);
        NetworkManager.Instance?.DisconnectFromLobby();
    }

    private void Back()
    {
        SwitchToMenu(BlastiaGame.MultiplayerMenu);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!_isDrawingStatusText || _statusTextUi == null)
            foreach (var codeEnterUi in _codeEnterElements)
                codeEnterUi.Draw(spriteBatch);
        else
            foreach (var statusTextUi in _statusTextElements)
                statusTextUi.Draw(spriteBatch);
    }
}