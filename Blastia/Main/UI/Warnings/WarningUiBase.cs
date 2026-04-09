using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI.Warnings;

public abstract class WarningUiBase : UIElement
{   
    private Image _warningImg;
    private Vector2 _textDrawPos;
    /// <summary>
    /// Spacing between image and text
    /// </summary>
    private const float TextSpacing = 12f;
    private const int GlowSmoothness = 12;
    
    private float _colorLerpFactor;
    private Color _startColor;
    private Color _endColor;
    private float _blinkSpeed;
    
    public WarningUiBase(Vector2 position, string text, Texture2D image, SpriteFont font, Color blinkAnimationStartColor, Color blinkAnimationEndColor, float blinkSpeed = 4f) : base(position, text, font)
    {
        _warningImg = new Image(position, image);
        
        Vector2 textScale = font.MeasureString(text) * Scale;
        float y = _warningImg.Bounds.Top + textScale.Y * 0.25f;
        _textDrawPos = new Vector2(_warningImg.Bounds.Right + TextSpacing, y);
        
        var bgHeight = Math.Max(textScale.Y, _warningImg.Bounds.Height);
            
        SetBackgroundProperties(() => new Rectangle(
            (int) Position.X + 5, 
            (int) Position.Y,
            (int) (_warningImg.Bounds.Width + TextSpacing + textScale.X) - 5,
            (int) bgHeight
        ), blinkAnimationStartColor, 0, blinkAnimationStartColor, -10f);
        
        CreateBackground();
        Background?.SetRightBorderImage(BlastiaGame.TextureManager.Get("RightBorderWing", "UI", "Background"));
        
        _startColor = blinkAnimationStartColor;
        _endColor = blinkAnimationEndColor;
        _blinkSpeed = blinkSpeed;
    }

    public override void Update()
    {
        if (Font == null) return;
        
        base.Update();
        
        float delta = (float) BlastiaGame.GameTimeElapsedSeconds;
        _colorLerpFactor += _blinkSpeed * delta;
        if (_colorLerpFactor >= 1)
            _colorLerpFactor = 0;
        
        Color lerpedColor = Util.Lerp(_startColor, _endColor, _colorLerpFactor);
        Background?.SetBackgroundColor(lerpedColor);
        Background?.SetBorderImagesColor(lerpedColor);
    }

    public override void UpdateBounds()
    {
        base.UpdateBounds();
        
        if (_warningImg == null || Font == null) return;
    
        _warningImg.Position = Position;
        _warningImg.UpdateBounds();
        
        Vector2 textScale = Font.MeasureString(Text) * Scale;
        float y = _warningImg.Bounds.Top + textScale.Y * 0.25f;
        _textDrawPos = new Vector2(_warningImg.Bounds.Right + TextSpacing, y);
        
        UpdateBackgroundPosition();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (Text == null) return;
        
        Background?.Draw(spriteBatch);
        base.DrawStringAt(spriteBatch, _textDrawPos, Text, scale: new Vector2(0.7f, 0.7f));
        
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