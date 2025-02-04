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
            new Tab("Close", BlastiaGame.RedCrossTexture, () => null, scale, Back),
            new Tab("Exit", BlastiaGame.ExitTexture, () => null, scale, ExitToMenu))
        {
            HAlign = 0.3f,
            VAlign = 0.3f
        };
        Elements.Add(tabs);
    }

    private void Back()
    {
        VideoManager.Instance.SaveStateToFile<VideoManagerState>();
        AudioManager.Instance.SaveStateToFile<AudioManagerState>();
        SwitchToMenu(BlastiaGame.InGameMenu);
    }

    private void ExitToMenu()
    {
        VideoManager.Instance.SaveStateToFile<VideoManagerState>();
        AudioManager.Instance.SaveStateToFile<AudioManagerState>();
        
        SwitchToMenu(BlastiaGame.MainMenu);
        if (BlastiaGame.LogoMenu != null) BlastiaGame.LogoMenu.Active = true;
    }
}