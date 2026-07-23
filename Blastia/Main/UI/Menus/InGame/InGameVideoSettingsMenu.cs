using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.InGame;

public class InGameVideoSettingsMenu(SpriteFont font, bool isActive = false) : Menu(font, isActive)
{
    protected override void AddElements()
    {
    }

    private void OnFullScreenClicked()
    {
        
    }

    private void OnResolutionClicked()
    {
        VideoManager.Instance.ApplyHandlerResolution();
    }
}