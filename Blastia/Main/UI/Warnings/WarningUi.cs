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
    private const float BgPadding = 3f;
    private const int GlowSmoothness = 12;
    
    public WarningUi(Vector2 position, string text, SpriteFont font) : base(position, text, font)
    {
        _warningImg = new Image(position, BlastiaGame.TextureManager.Rescale(BlastiaGame.TextureManager.Get("Warning", "UI", "Icons"), new Vector2(3f, 3f)));
        
        Vector2 textScale = font.MeasureString(text) * Scale;
        float y = _warningImg.Bounds.Top + textScale.Y * 0.25f;
        _textDrawPos = new Vector2(_warningImg.Bounds.Right + TextSpacing, y);
        
        var bgHeight = Math.Max(textScale.Y, _warningImg.Bounds.Height);
        var bgBounds = new Rectangle(
            (int) Position.X, 
            (int) Position.Y,
            (int) (_warningImg.Bounds.Width + TextSpacing + textScale.X),
            (int) bgHeight
        );
        
        // scale right border to fit the bg bounds height
        var rightBorderTex = BlastiaGame.TextureManager.Get("RightBorderWing", "UI", "Background");
        float factor = (bgHeight + 2 * BgPadding) / rightBorderTex.Height;
                    
        SetBackgroundProperties(() => bgBounds, Color.Black, 0, Color.Black, BgPadding);
        CreateBackground();
        Background?.SetRightBorderImage(BlastiaGame.TextureManager.Rescale(rightBorderTex, new Vector2(factor, factor)));
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
        
        for (int i = 0; i < GlowSmoothness; i++) 
        {
            float angle = MathHelper.TwoPi / GlowSmoothness * i;
            
            var offset = new Vector2(
                (float) Math.Cos(angle) * 3f,
                (float) Math.Sin(angle) * 3f
            );
            
            spriteBatch.Draw(_warningImg.Texture, _warningImg.Position + offset, Color.White * 0.1f);
        }
        
        _warningImg.Draw(spriteBatch);
    }
}