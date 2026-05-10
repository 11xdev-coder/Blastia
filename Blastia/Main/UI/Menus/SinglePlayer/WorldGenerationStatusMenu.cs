using Blastia.Main.GameState;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.SinglePlayer;

public class WorldGenerationStatusMenu(SpriteFont font, bool isActive = false) : Menu(font, isActive)
{
    private Progressbar? _progressBar;
    private Text? _statusText;

    protected override void AddElements()
    {
        _progressBar = new Progressbar(new Vector2(0, 600), BlastiaGame.TextureManager.Get("ProgressBarBG", "UI"), Color.White)
        {
            HAlign = 0.5f
        };
        Elements.Add(_progressBar);
        
        _statusText = new Text(new Vector2(0, 560), "Generating world", Font)
        {
            HAlign = 0.5f
        };
        Elements.Add(_statusText);
    }

    public override void Update()
    {
        base.Update();
        
        if (_progressBar == null) return;
        _progressBar.Progress = WorldGen.Progress;
    }
}