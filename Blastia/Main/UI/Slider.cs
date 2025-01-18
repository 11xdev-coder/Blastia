using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI;

public class Slider : Image
{
    private const string InitialPercentText = "100%";

    private readonly SliderHandle? _handle;
    private readonly Text? _percentText;
    private readonly Vector2 _percentTextSize;
    private readonly float _percentTextOffset;

    private readonly Action<float> _setValue;
    
    private float _percent;
    public float Percent
    {
        get => _percent;
        private set
        {
            _percent = Math.Clamp(value, 0, 1);
            _setValue(_percent);
            
            UpdatePercentTextAndPosition();
        }
    }

    public Slider(Vector2 position, SpriteFont font, 
        Func<float> getValue, Action<float> setValue,
        bool showPercent = false, float percentTextOffset = 35) : 
        base(position, BlastiaGame.SliderTexture)
    {
        _setValue = setValue;

        _handle = new SliderHandle(position, "O", font, this, () => Percent = CalculatePercent());
        
        _percentTextOffset = percentTextOffset;
        if (showPercent)
        {
            _percentText = new Text(Vector2.Zero, InitialPercentText, font);
            _percentTextSize = font.MeasureString(InitialPercentText);
        }

        Percent = getValue();
        _handle.Update();
    }
    
    /// <summary>
    /// Updates <c>_percentText</c> position
    /// </summary>
    private void UpdatePercentTextAndPosition()
    {
        if (_percentText == null) return;
        
        _percentText.Text = $"{Percent * 100:0.00}%";
        _percentText.Position = new Vector2(Bounds.Right + _percentTextOffset,
            Bounds.Center.Y - _percentTextSize.Y * 0.5f);
        _percentText.Update();
    }

    public override void UpdateBounds()
    {
        base.UpdateBounds();
        UpdatePercentTextAndPosition();
        
        _handle?.UpdateBounds();
        _percentText?.UpdateBounds();
    }

    private float CalculatePercent()
    {
        if (_handle == null) return Percent;

        var leftBound = _handle.CalculateSliderLeftBound();
        var rightBound = _handle.CalculateSliderRightBound();
        
        var relativeX = _handle.Position.X - leftBound;
        var width = rightBound - leftBound;
        float newPercent = relativeX / width;
        
        if (Math.Abs(_percent - newPercent) > 0.001f) // Only update if there's meaningful change
        {
            return newPercent;
        }

        return Percent;
    }
    
    public override void Update()
    {
        base.Update();
        
        _handle?.Update();
        _percentText?.Update();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
        
        _handle?.Draw(spriteBatch);
        _percentText?.Draw(spriteBatch);
    }
}