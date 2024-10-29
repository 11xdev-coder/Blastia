using Blastia.Main.UI.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.Settings;

public class VideoSettingsMenu : Menu
{
    public VideoSettingsMenu(SpriteFont font, bool isActive = false) : base(font, isActive)
    {
        AddElements();
    }

    private void AddElements()
    {
        HandlerArrowButton<DisplayMode> resolutionSwitcher = new HandlerArrowButton<DisplayMode>(new Vector2(0, 550), "Resolution", Font,
            OnClickResolution, 10, VideoManager.Instance.ResolutionHandler)
        {
            HAlign = 0.5f
        };
        resolutionSwitcher.AddToElements(Elements);
        
        BoolSwitchButton isFullScreenButton = new BoolSwitchButton(new Vector2(0, 600), "Full Screen", Font, 
            OnClickFullScreen, 
            () => VideoManager.Instance.IsFullScreen,
            _ => VideoManager.Instance.ToggleFullscreen())
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
        SwitchToMenu(BlastiaGame.SettingsMenu);
        VideoManager.Instance.SaveStateToFile();
    }

    private void OnClickFullScreen()
    {
        
    }

    private void OnClickResolution()
    {
        VideoManager.Instance.ApplyHandlerResolution();
    }
}