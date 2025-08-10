using Blastia.Main.UI.Buttons;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Blastia.Main.UI.Menus.InGame;

public class InGameChatMenu(SpriteFont font, bool isActive = false) : Menu(font, isActive)
{
    private Text? _chatSayText;
    public Input? ChatInput;
    private ScrollableArea? _chat;

    public override bool BlockEscape => true;
    
    protected override void AddElements()
    {
        Viewport chatViewport = new Viewport(2000, 200);
        _chat = new ScrollableArea(Vector2.Zero, chatViewport)
        {
            HAlign = 0.01f,
            VAlign = 0.98f
        };
        Elements.Add(_chat);
        _chat.AddChild(new Text(Vector2.Zero, "testastadsadfS", Font));
        
        _chatSayText = new Text(Vector2.Zero, "Say:", Font)
        {
            HAlign = 0.01f,
            VAlign = 0.98f
        };
        Elements.Add(_chatSayText);
        
        ChatInput = new Input(new Vector2(100, 0), Font, true, defaultText: "...")
        {
            HAlign = 0f,
            VAlign = 0.98f,
            IsSignEditing = true,
            CharacterLimit = 144,
            WrapLength = 144
        };
        Elements.Add(ChatInput);
    }
    
    public override void Update()
    {
        base.Update();
        
        // enter pressed
        if (KeyboardHelper.IsKeyJustPressed(Keys.Enter)) 
        {
            // turn chat off
            TurnChatOff();
            
            // send message
            
        }
        else if (KeyboardHelper.IsKeyJustPressed(Keys.Escape)) 
        {
            // turn chat off without sending
            TurnChatOff();
        }
    }

    protected override void OnMenuActive()
    {
        base.OnMenuActive();

        if (ChatInput == null) return;
        ChatInput.IsFocused = true;
    }
    
    private void TurnChatOff() 
    {
        if (ChatInput == null) return;
        ChatInput.IsFocused = false; // unfocus        
        ChatInput?.SetText(""); // clear input

        SwitchToMenu(BlastiaGame.InGameSettingsButtonMenu);
    }
}