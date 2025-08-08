using Blastia.Main.UI.Buttons;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Blastia.Main.UI.Menus.InGame;

public class InGameMenu(SpriteFont font, bool isActive = false) : Menu(font, isActive)
{
    private Button? _settingsButton;
    private Text? _chatSayText;
    public Input? ChatInput;
    private bool _wasChatActive;

    private readonly float _defaultVerticalAlign = 0.98f;
    
    protected override void AddElements()
    {
        _settingsButton = new Button(Vector2.Zero, "Settings", Font, OpenSettings)
        {
            HAlign = 0.02f,
            VAlign = _defaultVerticalAlign
        };
        Elements.Add(_settingsButton);

        _chatSayText = new Text(Vector2.Zero, "Say:", Font)
        {
            HAlign = 0.01f,
            VAlign = _defaultVerticalAlign,
            Alpha = 0f
        };
        Elements.Add(_chatSayText);

        ChatInput = new Input(new Vector2(100, 0), Font, true, focusedByDefault: true, defaultText: "...")
        {
            HAlign = 0f,
            VAlign = 2f,
            IsSignEditing = true,
            CharacterLimit = 144,
            WrapLength = 144
        };
        Elements.Add(ChatInput);
    }

    public bool IsChatCurrentlyActive() => _chatSayText?.Alpha > 0;
    public bool WasChatActivePreviousFrame() => _wasChatActive;
    
    public override void Update()
    {
        base.Update();

        // if chat is off (A < 1)
        if (KeyboardHelper.IsKeyJustPressed(Keys.Enter) && _chatSayText?.Alpha < 1 && _settingsButton != null && _chatSayText != null && ChatInput != null) 
        {
            // turn chat on
            _settingsButton.VAlign = 2f;
            
            _chatSayText.Alpha = 1f;
            ChatInput.VAlign = _defaultVerticalAlign;
            ChatInput.IsFocused = true; // start typing
        }
        else if (IsChatCurrentlyActive()) // if chat is on (A > 0)
        {
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
    }
    
    private void TurnChatOff() 
    {
        if (_settingsButton == null || _chatSayText == null || ChatInput == null) return;
        
        _settingsButton.VAlign = _defaultVerticalAlign;
            
        _chatSayText.Alpha = 0f;
        ChatInput.VAlign = 2f;
        ChatInput.IsFocused = false; // unfocus
        
        ChatInput?.SetText(""); // clear input
    }

    private void OpenSettings()
    {
        SwitchToMenu(BlastiaGame.InGameSettingsMenu);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);

        // update flag
        _wasChatActive = IsChatCurrentlyActive();
    }
}