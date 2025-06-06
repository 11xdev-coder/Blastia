using System.Numerics;
using Blastia.Main.Networking;
using Blastia.Main.UI.Buttons;
using Microsoft.Xna.Framework.Graphics;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Blastia.Main.UI.Menus.Multiplayer;

public class JoinGameMenu(SpriteFont font, bool isActive = false) : Menu(font, isActive)
{
    private Input? _codeEntry;
    
    protected override void AddElements()
    {
        _codeEntry = new Input(Vector2.Zero, Font, defaultText: "Enter code...")
        {
            HAlign = 0.5f,
            VAlign = 0.5f,
        };
        Elements.Add(_codeEntry);
        
        var joinGame = new Button(Vector2.Zero, "Join", Font, JoinGame)
        {
            HAlign = 0.5f,
            VAlign = 0.55f,
        };
        Elements.Add(joinGame);
        
        var back = new Button(Vector2.Zero, "Back", Font, Back)
        {
            HAlign = 0.5f,
            VAlign = 0.6f,
        };
        Elements.Add(back);
    }

    private void JoinGame()
    {
        if (_codeEntry == null || _codeEntry.Text == null) return;
        
        NetworkManager.Instance?.JoinLobbyWithCode(_codeEntry.Text);
    }

    private void Back()
    {
        SwitchToMenu(BlastiaGame.MultiplayerMenu);
    }
}