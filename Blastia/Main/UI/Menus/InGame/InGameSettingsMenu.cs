using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.InGame;

public class InGameSettingsMenu : Menu
{
    public InGameSettingsMenu(SpriteFont font, bool isActive = false) : base(font, isActive)
    {
        AddElements();
    }

    private void AddElements()
    {
        var scale = new Vector2(1.4f);
        var tabs = new TabGroup(Vector2.Zero, 40, this,
            new Tab("Video", BlastiaGame.MonitorTexture, () => BlastiaGame.InGameVideoSettingsMenu, scale),
            new Tab("Audio", BlastiaGame.AudioTexture, () => BlastiaGame.InGameAudioSettingsMenu, scale))
        {
            HAlign = 0.35f,
            VAlign = 0.4f
        };
    }
}