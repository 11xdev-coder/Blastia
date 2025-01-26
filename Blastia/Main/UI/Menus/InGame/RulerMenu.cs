using Blastia.Main.GameState;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Menus.InGame;

public class RulerMenu(SpriteFont font, bool isActive = false) : Menu(font, isActive)
{
    public override bool CameraUpdate => true;
    private readonly Dictionary<RulerHighlight, Vector2> _highlightPositions = new();

    protected override void AddElements()
    {
        var rulerModeText = new Text(Vector2.Zero, "Ruler Mode", Font)
        {
            HAlign = 0.9f,
            VAlign = 0.98f,
            Scale = new Vector2(3f)
        };
        Elements.Add(rulerModeText);

        var placeStartText = new Text(Vector2.Zero, "V: Place starting point", Font)
        {
            HAlign = 1.025f,
            VAlign = 0.93f,
            Scale = new Vector2(0.7f)
        };
        Elements.Add(placeStartText);
        
        var placeEndText = new Text(Vector2.Zero, "B: Place ending point", Font)
        {
            HAlign = 1.0235f,
            VAlign = 0.9f,
            Scale = new Vector2(0.7f)
        };
        Elements.Add(placeEndText);

        var removeRulerText = new Text(Vector2.Zero, "RMB: Remove ruler", Font)
        {
            HAlign = 1.0225f,
            VAlign = 0.87f,
            Scale = new Vector2(0.7f)
        };
        Elements.Add(removeRulerText);
        
        // TODO: implement bindings
    }
    
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
    public override void Update(Camera playerCamera)
    {
        // Update stored positions
        foreach (var highlight in _highlightPositions.Keys)
        {
            highlight.SetPosition(_highlightPositions[highlight], playerCamera);
        }
        base.Update();
    }

    public Vector2 GetHighlightPosition(RulerHighlight highlight)
    {
        return _highlightPositions.TryGetValue(highlight, out Vector2 pos) ? pos : Vector2.Zero;
    }
}