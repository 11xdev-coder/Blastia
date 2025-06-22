using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus;

public class FpsCounterMenu(SpriteFont font, bool isActive = true) : Menu(font, isActive)
{
    private Text? _fpsText;

    private int _fps;
    private int _frameCounter;
    private double _fpsTimer;
    
    protected override void AddElements()
    {
        _fpsText = new Text(new Vector2(5, 5), "FPS: 0", Font);
        Elements.Add(_fpsText);
    }

    public override void Update()
    {
        // count fps
        _fpsTimer += BlastiaGame.GameTimeElapsedSeconds;
        _frameCounter += 1;
        if (_fpsTimer >= 1)
        {
            _fps = _frameCounter;
            _frameCounter = 0;
            _fpsTimer -= 1;
        }

        if (_fpsText != null) _fpsText.Text = $"FPS: {_fps}";

        base.Update();
    }
}