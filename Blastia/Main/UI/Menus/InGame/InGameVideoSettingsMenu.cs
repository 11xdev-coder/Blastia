using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.InGame;

public class InGameVideoSettingsMenu(SpriteFont font, bool isActive = false) : Menu(font, isActive)
{
    protected override void AddElements()
    {
        // TODO: Save
        AddResolutionHandler(0.2f, 0.2f, OnResolutionClicked);
        AddFullscreenSwitch(0.2f, 0.24f, OnFullScreenClicked);
    }

    private void OnFullScreenClicked()
    {
        
    }

    private void OnResolutionClicked()
    {
        VideoManager.Instance.ApplyHandlerResolution();
    }
}