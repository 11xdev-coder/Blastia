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
        AddResolutionHandler(0.5f, 0.52f, OnClickResolution);
        AddFullscreenSwitch(0.5f, 0.56f, OnClickFullScreen);
        
        Button backButton = new Button(new Vector2(0, 650), "Back", Font, OnClickBack)
        {
            HAlign = 0.5f
        };
        Elements.Add(backButton);
        
    }

    private void OnClickBack()
    {
        SwitchToMenu(BlastiaGame.SettingsMenu);
        VideoManager.Instance.SaveStateToFile<VideoManagerState>();
    }

    private void OnClickFullScreen()
    {
        
    }

    private void OnClickResolution()
    {
        VideoManager.Instance.ApplyHandlerResolution();
    }
}