using Blastia.Main.Sounds;
using Blastia.Main.UI.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.Settings;

public class AudioSettingsMenu(SpriteFont font, bool isActive = false) : Menu(font, isActive)
{
    protected override void AddElements()
    {
        // RANDOM
        Button randomMusicButton = new Button(new Vector2(0, 450), "Play random menu track", Font, PlayRandomMusic)
        {
            HAlign = 0.5f
        };
        Elements.Add(randomMusicButton);
        
        AddMasterVolumeSlider(0.55f, 0.48f);
        AddMusicVolumeSlider(0.55f, 0.52f);
        AddSoundVolumeSlider(0.55f, 0.56f);
        
        Button backButton = new Button(new Vector2(0, 650), "Back", Font, OnClickBack)
        {
            HAlign = 0.5f
        };
        Elements.Add(backButton);
    }

    private void PlayRandomMusic()
    {
        MusicID musicId = BlastiaGame.ChooseRandomMenuMusic();
        MusicEngine.PlayMusicTrack(musicId);
    }
    
    private void OnClickBack()
    {
        SwitchToMenu(BlastiaGame.SettingsMenu);
        AudioManager.Instance.SaveStateToFile<AudioManagerState>();
    }
}