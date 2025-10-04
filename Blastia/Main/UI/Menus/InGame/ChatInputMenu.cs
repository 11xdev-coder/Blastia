using Blastia.Main.UI.Buttons;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Blastia.Main.UI.Menus.InGame;

public class ChatInputMenu(SpriteFont font, bool isActive = false) : Menu(font, isActive)
{
    private Text? _chatSayText;
    public Input? ChatInput;

    public override bool BlockEscape => true;
    
    protected override void AddElements()
    {
        
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
            CharacterLimit = 999,
            WrapTextSize = 1800,
            MoveInsteadOfWrapping = true
        };
        Elements.Add(ChatInput);
    }
    
    public override void Update()
    {
        base.Update();
        
        // enter pressed
        if (KeyboardHelper.IsKeyJustPressed(Keys.Enter)) 
        {
            // send message
            var selectedPlayerName = PlayerNWorldManager.Instance.GetSelectedPlayerName();
            BlastiaGame.ChatMessagesMenu?.AddMessage(selectedPlayerName, ChatInput?.StringBuilder.ToString());
            
            // turn chat off
            TurnChatOff();
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

        BlastiaGame.ChatMessagesMenu?.RevealMessages();
    }
    
    private void TurnChatOff() 
    {
        if (ChatInput == null) return;
        ChatInput.IsFocused = false; // unfocus        
        ChatInput?.SetText(""); // clear input

        SwitchToMenu(BlastiaGame.InGameSettingsButtonMenu);
    }
}