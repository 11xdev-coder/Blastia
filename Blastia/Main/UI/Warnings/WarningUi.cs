using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Warnings;

public class WarningUi : UIElement
{
    private Image _warningImg;
    private Vector2 _textDrawPos;
    /// <summary>
    /// Spacing between image and text
    /// </summary>
    private const float TextSpacing = 12f;
    
    public WarningUi(Vector2 position, string text, SpriteFont font) : base(position, text, font)
    {
        _warningImg = new Image(position, BlastiaGame.TextureManager.Rescale(BlastiaGame.TextureManager.Get("Warning", "UI", "Icons"), new Vector2(3f, 3f)));
        _textDrawPos = new Vector2(_warningImg.Bounds.Right + TextSpacing, Position.Y);
        
        Vector2 textScale = font.MeasureString(text) * Scale;
        var bgBounds = new Rectangle(
            (int) Position.X, 
            (int) Position.Y,
            (int) (_warningImg.Bounds.Width + TextSpacing + textScale.X),
            (int) Math.Max(textScale.Y, _warningImg.Bounds.Height)
        );
            
        SetBackgroundProperties(() => bgBounds, Color.Black, 0, Color.Black, 10);
        CreateBackground();
    }

    public override void Update()
    {
        base.Update();
        _warningImg.Update();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (Text == null) return;
        
        Background?.Draw(spriteBatch);
        base.DrawStringAt(spriteBatch, _textDrawPos, Text);
        _warningImg.Draw(spriteBatch);
    }
}