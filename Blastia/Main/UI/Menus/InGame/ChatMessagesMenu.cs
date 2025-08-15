using Blastia.Main.UI.Buttons;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Blastia.Main.UI.Menus.InGame;

public class ChatMessagesMenu(SpriteFont font, bool isActive = true) : Menu(font, isActive)
{
    public override ActivationMethod ActivationType => ActivationMethod.OnlyInGame;
    private ScrollableArea? _chat;
    private bool _isFading;
    private float _startFadingTimer;
    private const float SecondsToStartFadingOut = 4; // 4 seconds
    
    protected override void AddElements()
    {
        Viewport chatViewport = new Viewport(2000, 200);
        _chat = new ScrollableArea(new Vector2(15, 0), chatViewport, AlignmentType.Left)
        {
            HAlign = 0.01f,
            VAlign = 0.9f
        };
        Elements.Add(_chat);
    }
    
    public void AddMessage(string senderName, string? text) 
    {
        if (_chat == null) return;

        var newText = new Text(Vector2.Zero, $"<{senderName}>: {text}", Font);
        _chat.AddChild(newText);

        // setup timer
        _startFadingTimer = SecondsToStartFadingOut;
        _isFading = false;
        // reset alpha
        foreach (var child in _chat.Children)
        {
            child.LerpAlphaToZero = false;
            child.Alpha = 1f;
        }
    }

    public override void Update()
    {
        base.Update();

        if (_chat == null) return;
        
        var delta = (float)BlastiaGame.GameTimeElapsedSeconds;
        _startFadingTimer -= delta;
        
        // fade out
        if (_startFadingTimer <= 0 && !_isFading) // only do it one time
        {
            foreach (var child in _chat.Children)
                child.LerpAlphaToZero = true;
                
            _isFading = true;
        }
    }
}