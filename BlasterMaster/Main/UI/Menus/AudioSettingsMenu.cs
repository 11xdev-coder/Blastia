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
        Text soundVolumeText = new Text(new Vector2(0, 600), "Sound Volume", Font)
        {
            HAlign = 0.45f
        };
        Elements.Add(soundVolumeText);

        Slider soundsVolumeSlider = new Slider(new Vector2(0, 600), Font)
        {
            HAlign = 0.55f
        };
        Elements.Add(soundsVolumeSlider);
        
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