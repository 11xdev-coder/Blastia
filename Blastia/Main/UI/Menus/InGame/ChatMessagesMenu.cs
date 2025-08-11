using Blastia.Main.UI.Buttons;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Blastia.Main.UI.Menus.InGame;

public class ChatMessagesMenu(SpriteFont font, bool isActive = true) : Menu(font, isActive)
{
    private ScrollableArea? _chat;
    
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
    }
}