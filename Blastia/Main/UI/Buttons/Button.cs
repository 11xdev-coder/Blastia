using Blastia.Main.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Buttons;

public class Button : UIElement
{
    public Color NormalColor = Color.White;
    public Color SelectedColor = Color.Yellow;
    private ColoredBackground? _background;
    private bool _hasBackground;
    private Color _backgroundColor;
    private float _borderThickness;
    private Color _borderColor;

    public Button(Vector2 position, string text, SpriteFont font, Action? onClick) : 
        base(position, text, font)
    {
        OnClick += onClick;
        
        DrawColor = NormalColor;
        
        OnStartHovering = () => { PlayTickSound(); Select(); };
        OnEndHovering = Deselect;
    }
    
    /// <summary>
    /// Creates a button with custom background
    /// </summary>
    public Button(Vector2 position, string text, SpriteFont font, Action? onClick, Color backgroundColor, float borderThickness, Color borderColor) : 
        this(position, text, font, onClick)
    {
        _hasBackground = true;
        _backgroundColor = backgroundColor;
        _borderThickness = borderThickness;
        _borderColor = borderColor;
    }

    public override void OnAlignmentChanged()
    {
        base.OnAlignmentChanged();
        
        if (_background == null) return;
        _background.Position = new Vector2(Bounds.Left - 10, Bounds.Top - 10);
    }

    public override void UpdateBounds()
    {
        base.UpdateBounds();
        
        if (_hasBackground && _background == null) 
        {
            _background = new ColoredBackground(new Vector2(Bounds.Left - 10, Bounds.Top - 10), Bounds.Width + 20, Bounds.Height + 20, _backgroundColor, _borderThickness, _borderColor);
        }
    }

    private void Select()
    {
        DrawColor = SelectedColor;
    }

    private void Deselect()
    {
        DrawColor = NormalColor;
    }

    public override void Update()
    {
        _background?.Update();
        base.Update();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        _background?.Draw(spriteBatch);
        base.Draw(spriteBatch);
    }
}