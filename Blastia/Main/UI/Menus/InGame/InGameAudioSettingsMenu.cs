using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.InGame;

public class InGameAudioSettingsMenu(SpriteFont font, bool isActive = false) : Menu(font, isActive)
{
    protected override void AddElements()
    {
        AddMasterVolumeSlider(0.24f, 0.2f);
        AddMusicVolumeSlider(0.24f, 0.24f);
        AddSoundVolumeSlider(0.24f, 0.28f);
    }
}
