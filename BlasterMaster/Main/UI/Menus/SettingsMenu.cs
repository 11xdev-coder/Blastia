using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI.Menus;

public class SettingsMenu : Menu
{
    public SettingsMenu(SpriteFont font, bool isActive = false) : base(font, isActive)
    {
        AddElements();
    }

    private void AddElements()
    {
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
        SwitchToMenu(BlasterMasterGame.MainMenu);
    }

    private void OnClickVideoSettings()
    {
        VideoManager.Instance.LoadStateFromFile();
        SwitchToMenu(BlasterMasterGame.VideoSettingsMenu);
    }
}