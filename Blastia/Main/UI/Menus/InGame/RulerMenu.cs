using Blastia.Main.GameState;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.InGame;

public class RulerMenu(SpriteFont font, bool isActive = false) : Menu(font, isActive)
{
    private readonly Dictionary<RulerHighlight, Vector2> _highlightPositions = new();

    /// <summary>
    /// Adds <c>RulerHighlight</c> with its position to the dictionary
    /// </summary>
    /// <param name="highlight"></param>
    /// <param name="position"></param>
    /// <param name="playerCamera"></param>
    public void AddHighlight(RulerHighlight highlight, Vector2 position, Camera playerCamera)
    {
        Elements.Add(highlight);
        _highlightPositions[highlight] = position;
        highlight.SetPosition(position, playerCamera);
    }

    /// <summary>
    /// Updates <c>RulerHighlight</c> position
    /// </summary>
    /// <param name="highlight"></param>
    /// <param name="newPosition"></param>
    /// <param name="playerCamera"></param>
    public void UpdateHighlightPosition(RulerHighlight highlight, Vector2 newPosition, Camera playerCamera)
    {
        if (_highlightPositions.ContainsKey(highlight))
        {
            _highlightPositions[highlight] = newPosition;
            highlight.SetPosition(newPosition, playerCamera);
        }
    }

    /// <summary>
    /// Automatically gets <c>Player</c> from the <c>BlastiaGame</c> and updates all <c>RulerHighlights</c> with their
    /// positions
    /// </summary>
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