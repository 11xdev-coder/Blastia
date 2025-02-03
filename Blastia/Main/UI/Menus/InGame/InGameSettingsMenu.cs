using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.InGame;

public class InGameSettingsMenu(SpriteFont font, bool isActive = false) : Menu(font, isActive)
{
    protected override void AddElements()
    {
        var scale = new Vector2(1.2f);
        var tabs = new TabGroup(Vector2.Zero, 40,
            new Tab("Video", BlastiaGame.MonitorTexture, () => BlastiaGame.InGameVideoSettingsMenu, scale),
            new Tab("Audio", BlastiaGame.AudioTexture, () => BlastiaGame.InGameAudioSettingsMenu, scale),
            new Tab("Close", BlastiaGame.RedCrossTexture, () => null, scale, Back))
        {
            HAlign = 0.3f,
            VAlign = 0.3f
        };
        Elements.Add(tabs);
    }

    private void Back()
    {
        SwitchToMenu(BlastiaGame.InGameMenu);
    }
}