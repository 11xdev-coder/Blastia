using Blastia.Main.Networking;
using Blastia.Main.UI.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Steamworks;

namespace Blastia.Main.UI.Menus.InGame;

public class InGameSignEditMenu(SpriteFont font, bool isActive = false) : Menu(font, isActive)
{
    public Vector2 SignPosition { get; set; } = Vector2.Zero;
    public Input? SignText;
    private Button? _closeButton;
    
    protected override void AddElements()
    {
        var background = new Image(Vector2.Zero, BlastiaGame.TextureManager.Get("SignEditBackground", "UI"))
        {
            HAlign = 0.5f,
            VAlign = 0.3f
        };
        Elements.Add(background);

        // use pos instead of alignment for keeping in the same place
        SignText = new Input(new Vector2(710, 270), Font, true)
        {
            IsSignEditing = true,
            Scale = new Vector2(0.8f, 0.8f)
        };
        Elements.Add(SignText);
        
        var save = new Button(Vector2.Zero, "Save", Font, SaveText)
        {
            HAlign = 0.38f,
            VAlign = 0.42f
        };
        Elements.Add(save);
        
        _closeButton = new Button(Vector2.Zero, "Close", Font, CloseMenu)
        {
            HAlign = 0.44f,
            VAlign = 0.42f
        };
        Elements.Add(_closeButton);
    }

    private void SaveText()
    {
        var worldState = PlayerNWorldManager.Instance.SelectedWorld;
        if (worldState == null || SignPosition == Vector2.Zero || SignText == null) return;

        // set world state sign text
        var newText = SignText.StringBuilder.ToString();
        worldState.SignTexts[SignPosition] = newText; // set locally
        Active = false;

        // sync the sign edit
        var signEdited = new NetworkSignEditedMessage
        {
            Position = SignPosition,
            NewText = newText
        };
        NetworkSync.Sync(signEdited, MessageType.SignEditedAt, SyncMode.Auto);
    }

    private void CloseMenu()
    {
        Active = false;
        
        // fallback to original text
        var worldState = PlayerNWorldManager.Instance.SelectedWorld;
        if (SignText == null || worldState == null) return;
        
        worldState.SignTexts.TryGetValue(SignPosition, out var originalText);
        if (string.IsNullOrEmpty(originalText)) // fallback to empty string if not saved
            originalText = "";
        SignText.SetText(originalText);
    }

    public void UpdateText()
    {
        var worldState = PlayerNWorldManager.Instance.SelectedWorld;
        if (SignText == null || worldState == null) return;
        
        worldState.SignTexts.TryGetValue(SignPosition, out var text);
        SignText.SetText(text ?? "");
    }

    public override void Update()
    {
        base.Update();
        
        var worldState = PlayerNWorldManager.Instance.SelectedWorld;
        if (_closeButton == null || worldState == null) return;
        worldState.SignTexts.TryGetValue(SignPosition, out var originalText);
        var currentText = SignText?.StringBuilder.ToString();
        _closeButton.Text = currentText != originalText ? "Discard" : "Close";
    }
}