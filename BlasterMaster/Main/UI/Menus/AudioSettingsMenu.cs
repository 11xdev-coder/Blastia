using BlasterMaster.Main.Sounds;
using BlasterMaster.Main.UI.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI.Menus;

public class AudioSettingsMenu : Menu
{
    public AudioSettingsMenu(SpriteFont font, bool isActive = false) : base(font, isActive)
    {
        AddElements();
    }

    private void AddElements()
    {
        // RANDOM
        Button randomMusicButton = new Button(new Vector2(0, 450), "Play random menu track", Font, PlayRandomMusic)
        {
            HAlign = 0.5f
        };
        Elements.Add(randomMusicButton);
        
        // MASTER
        Text masterVolumeText = new Text(new Vector2(0, 500), "Master Volume", Font)
        {
            HAlign = 0.42f
        };
        Elements.Add(masterVolumeText);
        Slider masterVolumeSlider = new Slider(new Vector2(0, 500), Font,
            () => AudioManager.Instance.MasterVolume,
            f => AudioManager.Instance.MasterVolume = f, true)
        {
            HAlign = 0.5f
        };
        masterVolumeSlider.AddToElements(Elements);
        
        // MUSIC
        Text musicVolumeText = new Text(new Vector2(0, 550), "Music Volume", Font)
        {
            HAlign = 0.42f
        };
        Elements.Add(musicVolumeText);
        Slider musicVolumeSlider = new Slider(new Vector2(0, 550), Font,
            () => AudioManager.Instance.MusicVolume,
            f => AudioManager.Instance.MusicVolume = f, true)
        {
            HAlign = 0.5f
        };
        musicVolumeSlider.AddToElements(Elements);
        
        // SOUND
        Text soundVolumeText = new Text(new Vector2(0, 600), "Sound Volume", Font)
        {
            HAlign = 0.42f
        };
        Elements.Add(soundVolumeText);
        Slider soundsVolumeSlider = new Slider(new Vector2(0, 600), Font,
            () => AudioManager.Instance.SoundsVolume,
            f => AudioManager.Instance.SoundsVolume = f, true)
        {
            HAlign = 0.5f
        };
        soundsVolumeSlider.AddToElements(Elements);
        
        Button backButton = new Button(new Vector2(0, 650), "Back", Font, OnClickBack)
        {
            HAlign = 0.5f
        };
        Elements.Add(backButton);
    }

    private void PlayRandomMusic()
    {
        MusicID musicId = BlasterMasterGame.ChooseRandomMenuMusic();
        MusicEngine.PlayMusicTrack(musicId);
    }
    
    private void OnClickBack()
    {
        SwitchToMenu(BlasterMasterGame.SettingsMenu);
        AudioManager.Instance.SaveStateToFile();
    }
}