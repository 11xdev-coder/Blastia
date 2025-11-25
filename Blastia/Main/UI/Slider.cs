using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI;

public class Slider : Image, IValueStorageUi<float>
{
    private const string InitialPercentText = "100%";

    private readonly SliderHandle? _handle;
    private readonly Text? _percentText;
    private readonly Vector2 _percentTextSize;
    private readonly float _percentTextOffset;

    public Func<float>? GetValue { get; set; }
    public Action<float>? SetValue { get; set; }
    
    private float _percent;
    public float Percent
    {
        get => _percent;
        private set
        {
            _percent = Math.Clamp(value, 0, 1);
            SetValue?.Invoke(_percent);
            
            UpdatePercentTextAndPosition();
        }
    }

    public Slider(Vector2 position, SpriteFont font, 
        Func<float> getValue, Action<float> setValue, Action<Action>? subscribeToEvent = null,
        bool showPercent = false, float percentTextOffset = 35) : 
        base(position, BlastiaGame.TextureManager.Get("SliderBG", "UI"))
    {
        SetValue = setValue;
        GetValue = getValue;
        Percent = getValue();
        
        _handle = new SliderHandle(position, "O", font, this, () => Percent = CalculatePercent());
        
        _percentTextOffset = percentTextOffset;
        if (showPercent)
        {
            _percentText = new Text(Vector2.Zero, InitialPercentText, font);
            _percentTextSize = font.MeasureString(InitialPercentText);
        }
        
        if (subscribeToEvent != null) subscribeToEvent(UpdateLabel);
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
    
    public void UpdateLabel()
    {
        if (GetValue != null) Percent = GetValue();
        UpdatePercentTextAndPosition();
    }
}