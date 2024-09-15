using BlasterMaster.Main.UI.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI;

public class Slider : Button
{
    public override bool Draggable => true;

    private Image? _backgroundImage;
    private float _bgTextureHeight;
    // flag to update background position only once
    private bool _hasUpdatedBgPosition;

    private const string SliderText = "O";
    private float _sliderTextSizeX;
    private float _sliderBgLeftBound;
    private float _sliderBgRightBound;

    private const string InitialPercentText = "100%";
    private Text? _percentText;
    private bool _hasUpdatedPercentText;
    private bool _showPercentText;
    private Vector2 _percentTextSize;
    private float _percentTextOffset;

    private bool _updatedPercent = true;
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
        }
    }

    public Slider(Vector2 position, SpriteFont font, 
        Func<float> getValue, Action<float> setValue,
        bool showPercent = false, float percentTextOffset = 35) : 
        base(position, SliderText, font, null)
    {
        // initialize BG image, get its Heights
        InitBackgroundImage(position);
        
        // get text size of "O"
        InitSliderTextSize();

        if (showPercent)
        {
            // if showPercent = true, initialize _percentText with custom pos,
            // get size of 100% and init _percentTextOffset
            InitPercentText(position, percentTextOffset);
        }
        
        _getValue = getValue;
        _setValue = setValue;
        InitPercent();
    }

    /// <summary>
    /// Initializes the background image for the slider.
    /// </summary>
    /// <param name="pos">The position where the background image will be placed.</param>
    private void InitBackgroundImage(Vector2 pos)
    {
        _backgroundImage = new Image(pos, BlasterMasterGame.SliderTexture);
        _bgTextureHeight = _backgroundImage.Texture.Height;
    }

    /// <summary>
    /// Initializes the text size for the slider.
    /// </summary>
    private void InitSliderTextSize()
    {
        _sliderTextSizeX = Font.MeasureString(SliderText).X;
    }

    /// <summary>
    /// Initializes the percent text display for the slider.
    /// </summary>
    /// <param name="pos">The position of the slider.</param>
    /// <param name="offset">The offset distance for the percent text from the slider.</param>
    private void InitPercentText(Vector2 pos, float offset)
    {
        _showPercentText = true;
        Vector2 percentTextPosition = CalculatePercentTextPosition(pos);
        _percentText = new Text(percentTextPosition, InitialPercentText, Font);
        _percentTextSize = Font.MeasureString(InitialPercentText);
        _percentTextOffset = offset;
    }

    /// <summary>
    /// Initializes the percentage for the slider based on the value from the getValue function.
    /// </summary>
    private void InitPercent()
    {
        float value = _getValue();
        _percent = value;
        _updatedPercent = false; // request slider update
        Console.WriteLine($"Slider loaded percent: {_percent}");
    }

    /// <summary>
    /// Gets the position for the percent text relative to the slider's background image.
    /// </summary>
    /// <param name="initialPos">The initial slider position.</param>
    /// <return>The calculated position of the percent text.</return>
    private Vector2 CalculatePercentTextPosition(Vector2 initialPos = default)
    {
        if (_backgroundImage != null)
        {
            if (_hasUpdatedBgPosition)
            {
                return CalculatePercentTextPositionForUpdatedBg();
            }
            
            return new Vector2(_backgroundImage.Bounds.Right, initialPos.Y);
        }
        
        return new Vector2(initialPos.X, initialPos.Y);
    }

    /// <summary>
    /// Calculates the position of the percent text when the background has been updated.
    /// </summary>
    /// <returns>The position vector for the percent text.</returns>
    private Vector2 CalculatePercentTextPositionForUpdatedBg()
    {
        return new Vector2(_sliderBgRightBound + _percentTextOffset,
            Bounds.Center.Y - _percentTextSize.Y * 0.5f);
    }

    public override void Update()
    {
        base.Update();
        
        // update percent text
        if(_percentText != null && _showPercentText) 
            _percentText.Text = $"{Percent * 100:0.00}%";
    }

    public override void UpdateBounds()
    {
        base.UpdateBounds();
        UpdateBackgroundImage();
        UpdatePercentText();
        UpdateSliderPosition();
    }

    private void UpdateBackgroundImage()
    {
        if (_backgroundImage == null || _hasUpdatedBgPosition) return;
        
        // if BG image is init, and we didnt update
        // set pos to half of the "O"
        Vector2 bgPosition = CalculateBgImagePosition();
        
        // set custom bounds calculated with size of "O"
        UpdateBgAndSetBounds(bgPosition);
        
        _hasUpdatedBgPosition = true;
    }

    /// <summary>
    /// Calculates the position of the background image relative to the slider.
    /// </summary>
    /// <returns>
    /// A <see cref="Vector2"/> representing the calculated position of the background image.
    /// </returns>
    private Vector2 CalculateBgImagePosition()
    {
        float x = Bounds.Left + _sliderTextSizeX * 0.5f;
        float y = Bounds.Center.Y - _bgTextureHeight * 0.65f;
        return new Vector2(x, y);
    }

    /// <summary>
    /// Updates the background image position and sets the bounds for the slider.
    /// </summary>
    /// <param name="position">The position to set the background image to.</param>
    private void UpdateBgAndSetBounds(Vector2 position)
    {
        if(_backgroundImage == null || _hasUpdatedBgPosition) return;
        
        _backgroundImage.Position = position;
        _backgroundImage.UpdateBounds();
        
        // set bounds
        _sliderBgLeftBound = _backgroundImage.Bounds.Left - _sliderTextSizeX * 0.5f;
        _sliderBgRightBound = _backgroundImage.Bounds.Right - _sliderTextSizeX * 0.5f;
    }

    private void UpdatePercentText()
    {
        if (_percentText != null && _showPercentText &&
            !_hasUpdatedPercentText && _hasUpdatedBgPosition && _backgroundImage != null)
        {
            // if _percentText is init, if we need to show it and didnt updated it, and init BG image
            // set position to the right of the slider
            _percentText.Position = CalculatePercentTextPosition();
            
            _hasUpdatedPercentText = true;
        }
    }

    public void AddToElements(List<UIElement> elements)
    {
        if (_backgroundImage != null) elements.Add(_backgroundImage);
        if (_percentText != null) elements.Add(_percentText);
        elements.Add(this);
    }

    private void UpdateSliderPosition()
    {
        if (!_hasUpdatedBgPosition || _updatedPercent) return;
        
        float rightBound = _sliderBgRightBound - 1;
        float newX = _sliderBgLeftBound + _percent * (rightBound - _sliderBgLeftBound);
        Position = new Vector2(newX, Position.Y);
        FixHAlignment(); // align properly
        
        _updatedPercent = true;
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
}