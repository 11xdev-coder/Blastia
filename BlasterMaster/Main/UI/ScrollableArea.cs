using System.Reflection.Metadata.Ecma335;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI;

public class ScrollableArea : UIElement
{
    private List<UIElement> _children;
    // position offset
    private float _scrolledOffset;

    public int ViewportWidth;
    public int ViewportHeight;

    /// <summary>
    /// Number of pixels scrolled per wheel tick
    /// </summary>
    public float ScrollSpeed { get; set; } = 0.05f;
    
    public ScrollableArea(Vector2 position, Viewport viewport, 
        float scrolledOffset = 0f) : 
        base(position, BlasterMasterGame.InvisibleTexture)
    {
        ViewportWidth = viewport.Width;
        ViewportHeight = viewport.Height;
        
        _children = new List<UIElement>();
        _scrolledOffset = scrolledOffset;
    }

    public override void UpdateBounds()
    {
        UpdateBoundsBase(ViewportWidth, ViewportHeight);
    }

    public void AddChild(UIElement child)
    {
        _children.Add(child);
    }

    public override void Update()
    {
        base.Update();

        _scrolledOffset -= BlasterMasterGame.ScrollWheelDelta * ScrollSpeed;
        Console.WriteLine(_scrolledOffset);
        
        // update every child
        foreach (var child in _children)
        {
            child.Position.Y = _scrolledOffset;
            
            child.Update();
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
        
        // draw if in the viewport
        foreach (var child in _children)
        {
            if (Bounds.Intersects(child.Bounds))
            {
                child.Draw(spriteBatch);
            }
        }
    }
}

public class Viewport(int width, int height)
{
    public readonly int Width = width;
    public readonly int Height = height;
}