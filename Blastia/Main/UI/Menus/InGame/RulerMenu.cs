using Blastia.Main.GameState;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.InGame;

public class RulerMenu(SpriteFont font, bool isActive = false) : Menu(font, isActive)
{
    private readonly Dictionary<RulerHighlight, Vector2> _highlightPositions = new();

    public void AddHighlight(RulerHighlight highlight, Vector2 position, Camera playerCamera)
    {
        Elements.Add(highlight);
        _highlightPositions[highlight] = position;
        highlight.SetPosition(position, playerCamera);
    }

    public void UpdateHighlightPosition(RulerHighlight highlight, Vector2 newPosition, Camera playerCamera)
    {
        if (_highlightPositions.ContainsKey(highlight))
        {
            _highlightPositions[highlight] = newPosition;
            highlight.SetPosition(newPosition, playerCamera);
        }
    }

    public override void Update()
    {
        var player = BlastiaGame.RequestPlayer();
        if (player?.Camera == null) return;
        
        // Update stored positions
        foreach (var highlight in _highlightPositions.Keys)
        {
            highlight.SetPosition(_highlightPositions[highlight], player.Camera);
        }
        base.Update();
    }

    public Vector2 GetHighlightPosition(RulerHighlight highlight)
    {
        return _highlightPositions.TryGetValue(highlight, out Vector2 pos) ? pos : Vector2.Zero;
    }
}