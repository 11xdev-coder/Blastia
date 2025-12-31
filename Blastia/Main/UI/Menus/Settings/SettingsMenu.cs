using Blastia.Main.UI.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.Settings;

public class SettingsMenu(SpriteFont font, bool isActive = false) : Menu(font, isActive)
{
    protected override void AddElements()
    {
        Button audioButton = new Button(new Vector2(0, 550), "Audio Settings", Font, OnClickAudioSettings)
        {
            HAlign = 0.5f
        };
        Elements.Add(audioButton);
        
        Button videoButton = new Button(new Vector2(0, 600), "Video Settings", Font, OnClickVideoSettings)
        {
            HAlign = 0.5f
        };
        Elements.Add(videoButton);
        
        Button backButton = new Button(new Vector2(0, 650), "Back", Font, OnClickBack)
        {
            HAlign = 0.5f
        };
        Elements.Add(backButton);
    }

    private void OnClickBack()
    {
        SwitchToMenu(BlastiaGame.MainMenu);
    }

    private void OnClickAudioSettings()
    {
        AudioManager.Instance.LoadStateFromFile<AudioManagerState>();
        SwitchToMenu(BlastiaGame.AudioSettingsMenu);
    }

    private void OnClickVideoSettings()
    {
        VideoManager.Instance.LoadStateFromFile<VideoManagerState>();
        SwitchToMenu(BlastiaGame.VideoSettingsMenu);
    }
}