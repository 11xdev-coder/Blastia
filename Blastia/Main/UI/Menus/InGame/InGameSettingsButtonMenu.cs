using Blastia.Main.UI.Buttons;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Blastia.Main.UI.Menus.InGame;

public class InGameSettingsButtonMenu(SpriteFont font, bool isActive = false) : Menu(font, isActive)
{
    private Button? _settingsButton;

    public override ActivationMethod ActivationType => ActivationMethod.OnlyInGame;
    
    protected override void AddElements()
    {
        _settingsButton = new Button(Vector2.Zero, "Settings", Font, OpenSettings)
        {
            HAlign = 0.02f,
            VAlign = 0.98f
        };
        Elements.Add(_settingsButton);
    }

    public override void Update()
    {
        base.Update();

        if (KeyboardHelper.IsKeyJustPressed(Keys.Enter))
            SwitchToMenu(BlastiaGame.ChatInputMenu);
    }

    private void OpenSettings()
    {
        SwitchToMenu(BlastiaGame.InGameSettingsMenu);
    }
}