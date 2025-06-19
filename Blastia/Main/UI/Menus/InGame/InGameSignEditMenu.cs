using Blastia.Main.UI.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.InGame;

public class InGameSignEditMenu(SpriteFont font, bool isActive = false) : Menu(font, isActive)
{
    public Vector2 SignPosition { get; set; } = Vector2.Zero;
    private Input? _signText;
    private Button? _closeButton;
    
    protected override void AddElements()
    {
        var background = new Image(Vector2.Zero, BlastiaGame.SignEditBackgroundTexture)
        {
            HAlign = 0.5f,
            VAlign = 0.3f
        };
        Elements.Add(background);

        _signText = new Input(Vector2.Zero, Font, true)
        {
            HAlign = 0.62f,
            VAlign = 0.28f,
            IsSignEditing = true,
            Scale = new Vector2(0.8f, 0.8f)
        };
        Elements.Add(_signText);
        
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
        if (worldState == null || SignPosition == Vector2.Zero || _signText == null) return;
        // set world state sign text
        worldState.SignTexts[SignPosition] = _signText.StringBuilder.ToString();
        Active = false;
    }

    private void CloseMenu()
    {
        Active = false;
        
        // fallback to original text
        var worldState = PlayerNWorldManager.Instance.SelectedWorld;
        if (_signText == null || worldState == null) return;
        
        worldState.SignTexts.TryGetValue(SignPosition, out var originalText);
        if (string.IsNullOrEmpty(originalText)) // fallback to empty string if not saved
            originalText = "";
        _signText.SetText(originalText);
    }

    public void UpdateText()
    {
        var worldState = PlayerNWorldManager.Instance.SelectedWorld;
        if (_signText == null || worldState == null) return;
        
        worldState.SignTexts.TryGetValue(SignPosition, out var text);
        _signText.SetText(text ?? "");
    }

    public override void Update()
    {
        base.Update();
        
        var worldState = PlayerNWorldManager.Instance.SelectedWorld;
        if (_closeButton == null || worldState == null) return;
        worldState.SignTexts.TryGetValue(SignPosition, out var originalText);
        var currentText = _signText?.StringBuilder.ToString();
        _closeButton.Text = currentText != originalText ? "Discard" : "Close";
    }
}