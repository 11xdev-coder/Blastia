using Blastia.Main.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.InGame;

public class InGameSettingsMenu(SpriteFont font, bool isActive = false) : Menu(font, isActive)
{
    public override bool BlockEscape => true;
    private TabGroup? _tabGroup;
    protected override void AddElements()
    {
        var scale = new Vector2(1.2f);
        _tabGroup = new TabGroup(Vector2.Zero, 40,
            new Tab("Video", BlastiaGame.MonitorTexture, () => BlastiaGame.InGameVideoSettingsMenu, scale),
            new Tab("Audio", BlastiaGame.AudioTexture, () => BlastiaGame.InGameAudioSettingsMenu, scale),
            new Tab("Close", BlastiaGame.RedCrossTexture, () => null, scale, Back),
            new Tab("Exit", BlastiaGame.ExitTexture, () => null, scale, ExitToMenu))
        {
            HAlign = 0.3f,
            VAlign = 0.3f
        };
        Elements.Add(_tabGroup);
    }

    private void Back()
    {
        VideoManager.Instance.SaveStateToFile<VideoManagerState>();
        AudioManager.Instance.SaveStateToFile<AudioManagerState>();
        _tabGroup?.DeselectAll();
        SwitchToMenu(BlastiaGame.InGameSettingsButtonMenu);
    }

    private void ExitToMenu()
    {
        VideoManager.Instance.SaveStateToFile<VideoManagerState>();
        AudioManager.Instance.SaveStateToFile<AudioManagerState>();
        
        _tabGroup?.DeselectAll();
        SwitchToMenu(BlastiaGame.MainMenu);
        BlastiaGame.RequestWorldUnload();
        if (NetworkManager.Instance != null && NetworkManager.Instance.IsInMultiplayerSession)
        {
            NetworkManager.Instance.DisconnectFromLobby();
        }
    }
}