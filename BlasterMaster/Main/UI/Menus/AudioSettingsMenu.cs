using BlasterMaster.Main.UI.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
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
        // MASTER
        Text masterVolumeText = new Text(new Vector2(0, 500), "Master Volume", Font)
        {
            HAlign = 0.45f
        };
        Elements.Add(masterVolumeText);
        Slider masterVolumeSlider = new Slider(new Vector2(0, 500), Font,
            () => AudioManager.Instance.MasterVolume,
            f => AudioManager.Instance.MasterVolume = f, true)
        {
            HAlign = 0.55f
        };
        masterVolumeSlider.AddToElements(Elements);
        
        // MUSIC
        Text musicVolumeText = new Text(new Vector2(0, 550), "Music Volume", Font)
        {
            HAlign = 0.45f
        };
        Elements.Add(musicVolumeText);
        Slider musicVolumeSlider = new Slider(new Vector2(0, 550), Font,
            () => AudioManager.Instance.MusicVolume,
            f => AudioManager.Instance.MusicVolume = f, true)
        {
            HAlign = 0.55f
        };
        musicVolumeSlider.AddToElements(Elements);
        
        // SOUND
        Text soundVolumeText = new Text(new Vector2(0, 600), "Sound Volume", Font)
        {
            HAlign = 0.45f
        };
        Elements.Add(soundVolumeText);
        Slider soundsVolumeSlider = new Slider(new Vector2(0, 600), Font,
            () => AudioManager.Instance.SoundsVolume,
            f => AudioManager.Instance.SoundsVolume = f, true)
        {
            HAlign = 0.55f
        };
        soundsVolumeSlider.AddToElements(Elements);
        
        Button backButton = new Button(new Vector2(0, 650), "Back", Font, OnClickBack)
        {
            HAlign = 0.5f
        };
        Elements.Add(backButton);
    }

    private void OnClickBack()
    {
        SwitchToMenu(BlasterMasterGame.SettingsMenu);
        AudioManager.Instance.SaveStateToFile();
    }
}