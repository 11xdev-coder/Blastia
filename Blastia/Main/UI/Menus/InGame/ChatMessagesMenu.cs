using Blastia.Main.Networking;
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
        Viewport chatViewport = new Viewport(2000, 300);
        _chat = new ScrollableArea(new Vector2(15, 0), chatViewport, AlignmentType.Left)
        {
            HAlign = 0.01f,
            VAlign = 0.9f
        };
        Elements.Add(_chat);
    }
    
    public void AddMessage(string? senderName, string? text, bool shouldSyncToNetwork = true) 
    {
        if (_chat == null) return;

        // have a sender -> use <> brackets
        ColoredText newText;
        if (senderName != null)
            newText = new ColoredText(Vector2.Zero, $"<{senderName}>: {text}", Font);
        else
            newText = new ColoredText(Vector2.Zero, $"{text}", Font);
            
        newText.UpdateBounds(); // force update
        _chat.AddChild(newText);

        // scroll to bottom
        _chat.ScrollToBottom();
        RevealMessages();

        if (shouldSyncToNetwork) NetworkManager.Instance?.SyncChatMessage(text ?? "", senderName);
    }
    
    public void RevealMessages() 
    {
        if (_chat == null) return;
        
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
        
        if (_chat == null || _chat.Children.Count <= 0) return;
        
        // if messages are hidden -> block scrolling
        if (_chat.Children[0].Alpha == 0f) _chat.ScrollLocked = true;
        else _chat.ScrollLocked = false;

        // dont hide when chat input is active (typing a message)
        if (BlastiaGame.ChatInputMenu?.Active == true) return;
        
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