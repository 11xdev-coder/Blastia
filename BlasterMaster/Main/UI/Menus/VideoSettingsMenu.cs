using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI.Menus;

public class VideoSettingsMenu : Menu
{
    public VideoSettingsMenu(SpriteFont font, bool isActive = false) : base(font, isActive)
    {
        AddElements();
    }

    private void AddElements()
    {
        BoolSwitchButton isFullScreenButton = new BoolSwitchButton(new Vector2(0, 600), "Full Screen", Font, 
            OnClickFullScreen, 
            () => VideoManager.Instance.IsFullScreen,
            newBool => VideoManager.Instance.ToggleFullscreen())
        {
            HAlign = 0.5f
        };
        Elements.Add(isFullScreenButton);
        
        Button backButton = new Button(new Vector2(0, 650), "Back", Font, OnClickBack)
        {
            HAlign = 0.5f
        };
        Elements.Add(backButton);
        
    }

    private void OnClickBack()
    {
        SwitchToMenu(BlasterMasterGame.SettingsMenu);
    }

    private void OnClickFullScreen()
    {
        
    }
}