using BlasterMaster.Main.UI.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI;

public class Slider : Button
{
    public override bool Draggable => true;

    public override float HAlign
    {
        get => base.HAlign;
        set
        {
            base.HAlign = value;

            UpdateSliderVisuals();
        }
    }
    public override float VAlign
    {
        get => base.VAlign;
        set
        {
            base.VAlign = value;

            UpdateSliderVisuals();
        }
    }

    private const string InitialPercentText = "100%";
    private const string SliderText = "O";
    
    private Image? _backgroundImage;
    private float _sliderTextSizeX;
    
    private float _sliderBgLeftBound;
    private float _sliderBgRightBound;
    
    private Text? _percentText;
    private Vector2 _percentTextSize;
    private float _percentTextOffset;

    private Func<float> _getValue;
    private Action<float> _setValue;
    
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
        base(position, SliderText, font, null)
    {
        _backgroundImage = new Image(Position, BlasterMasterGame.SliderTexture);
        
        _sliderTextSizeX = font.MeasureString(SliderText).X;
        
        _getValue = getValue;
        _setValue = setValue;
        
        _percentTextOffset = percentTextOffset;
        _percentText = showPercent ? new Text(Vector2.Zero, InitialPercentText, font) : null;
        _percentTextSize = font.MeasureString(InitialPercentText);

        InitializeSlider();
    }
    
    /// <summary>
    /// Initializes the percentage for the slider based on the value from the getValue function.
    /// </summary>
    private void InitializeSlider()
    {
        Percent = _getValue();
        UpdateSliderPosition();
        
        Console.WriteLine($"Slider loaded percent: {Percent}");
    }

    private void UpdateBackgroundImagePositionAndBounds()
    {
        if (_backgroundImage == null) return;
        
        _backgroundImage.Position = CalculateBackgroundImagePosition();
        _backgroundImage.Update();
        
        _sliderBgLeftBound = _backgroundImage.Bounds.Left - _sliderTextSizeX * 0.5f;
        _sliderBgRightBound = _backgroundImage.Bounds.Right - _sliderTextSizeX * 0.5f;
    }
    
    private Vector2 CalculateBackgroundImagePosition()
    {
        if (_backgroundImage == null) return Position;
        
        float x = Bounds.Left + _sliderTextSizeX * 0.5f;
        float y = Bounds.Center.Y - _backgroundImage.Texture.Height * 0.65f;
        return new Vector2(x, y);
    }

    /// <summary>
    /// Updates background image position and bounds. Updates percent text and position.
    /// Updates slider position.
    /// </summary>
    private void UpdateSliderVisuals()
    {
        UpdateBackgroundImagePositionAndBounds();
        UpdatePercentTextAndPosition();
        UpdateSliderPosition();
    }

    /// <summary>
    /// Updates percent text, and position using offset.
    /// </summary>
    private void UpdatePercentTextAndPosition()
    {
        if (_percentText == null) return;
        
        _percentText.Text = $"{Percent * 100:0.00}%";
        _percentText.Position = new Vector2(_sliderBgRightBound + _percentTextOffset,
            Bounds.Center.Y - _percentTextSize.Y * 0.5f);
        _percentText.Update();
    }

    /// <summary>
    /// Updates slider position using Percent considering the HAlign
    /// </summary>
    private void UpdateSliderPosition()
    {
        float rightBound = _sliderBgRightBound - 1;
        float newX = _sliderBgLeftBound + Percent * (rightBound - _sliderBgLeftBound);
        float alignedX = GetAlignedPositionX(newX);
        
        Position = new Vector2(alignedX, Position.Y);
    }
    
    protected override void UpdateDragPosition(Vector2 cursorPosition)
    {
        // get drag X position (clamped to the BG)
        Vector2 newDrag = GetDragPositionNoYClamped(cursorPosition.X, 
            _sliderBgLeftBound, 
            _sliderBgRightBound); 
        Position = newDrag;
        
        // calculate percent
        Percent = CalculatePercent();
    }

    private float CalculatePercent()
    {
        float rightBound = _sliderBgRightBound - 1;
        return (Bounds.Left - _sliderBgLeftBound) / 
               (rightBound - _sliderBgLeftBound);
    }
    
    public override void Update()
    {
        base.Update();
        
        _backgroundImage?.Update();
        _percentText?.Update();
    }

    public override void UpdateBounds()
    {
        base.UpdateBounds();
        _backgroundImage?.UpdateBounds();
        _percentText?.UpdateBounds();
    }
    
    public void AddToElements(List<UIElement> elements)
    {
        if (_backgroundImage != null) elements.Add(_backgroundImage);
        if (_percentText != null) elements.Add(_percentText);
        elements.Add(this);
    }
}