using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.InGame;

public class InGameVideoSettingsMenu : Menu
{
    // TODO: TabGroup menu, AddElements not in constructor (to init HAlignOffset)
    public InGameVideoSettingsMenu(SpriteFont font, bool isActive = false) : base(font, isActive)
    {
        
    }

    protected override void AddElements()
    {
        AddFullscreenSwitch(HAlignOffset, VAlignOffset, () => {});
    }
}