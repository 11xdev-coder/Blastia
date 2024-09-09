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
}