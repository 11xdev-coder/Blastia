using BlasterMaster.Main.UI.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI;

public class Slider : Button
{
    public override bool Draggable => true;

    private readonly Image? _backgroundImage;
    private readonly float _bgTextureHeight;
    // flag to update background position only once
    private bool _hasUpdatedBgPosition;

    private readonly float _sliderTextSizeX;
    private float _sliderBgLeftBound;
    private float _sliderBgRightBound;

    private readonly Text? _percentText;
    private bool _hasUpdatedPercentText;
    private readonly bool _showPercentText;
    private readonly Vector2 _percentTextSize;
    private readonly float _percentTextOffset;

    public float Percent { get; private set; }

    public Slider(Vector2 position, SpriteFont font, bool showPercent = false, 
        float percentTextOffset = 35) : 
        base(position, "O", font, null)
    {
        // initialize BG image, get its Height
        _backgroundImage = new Image(position, BlasterMasterGame.SliderTexture);
        _bgTextureHeight = _backgroundImage.Texture.Height;
        
        // get text size of "O"
        _sliderTextSizeX = Font.MeasureString(Text).X;

        if (showPercent)
        {
            // if showPercent = true, initialize _percentText with custom pos,
            // get size of 100% and init _percentTextOffset
            _showPercentText = true;
            Vector2 percentTextPos = new Vector2(_backgroundImage.Bounds.Right, position.Y);
            _percentText = new Text(percentTextPos, "100%", Font);
            _percentTextSize = Font.MeasureString("100%");
            _percentTextOffset = percentTextOffset;
        }
    }

    public override void Update()
    {
        base.Update();
        
        // update percent text
        if(_percentText != null && _showPercentText) 
            _percentText.Text = Percent.ToString("0.00");
    }

    public override void UpdateBounds()
    {
        base.UpdateBounds();
        UpdateBackgroundImage();
        UpdatePercentText();
    }

    private void UpdateBackgroundImage()
    {
        if (_backgroundImage != null && !_hasUpdatedBgPosition)
        {
            // if BG image is init, and we didnt update
            // set pos to half of the "O"
            Vector2 position = new Vector2(Bounds.Left + _sliderTextSizeX * 0.5f, 
                Bounds.Center.Y - _bgTextureHeight * 0.65f);
            _backgroundImage.Position = position;
            _backgroundImage.UpdateBounds();
            
            // set custom bounds calculated with size of "O"
            _sliderBgLeftBound = _backgroundImage.Bounds.Left - _sliderTextSizeX * 0.5f;
            _sliderBgRightBound = _backgroundImage.Bounds.Right - _sliderTextSizeX * 0.5f;
            
            _hasUpdatedBgPosition = true;
        }
    }

    private void UpdatePercentText()
    {
        if (_percentText != null && _showPercentText &&
            !_hasUpdatedPercentText && _hasUpdatedBgPosition && _backgroundImage != null)
        {
            // if _percentText is init, if we need to show it and didnt updated it, and init BG image
            // set position to the right of the slider
            Vector2 position = new Vector2(_sliderBgRightBound + _percentTextOffset,
                Bounds.Center.Y - _percentTextSize.Y * 0.5f);
            _percentText.Position = position;
            
            _hasUpdatedPercentText = true;
        }
    }

    public void AddToElements(List<UIElement> elements)
    {
        if (_backgroundImage != null) elements.Add(_backgroundImage);
        if (_percentText != null) elements.Add(_percentText);
        elements.Add(this);
    }

    protected override void UpdateDragPosition(Vector2 cursorPosition)
    {
        // get drag X position (clamped to the BG)
        Vector2 newDrag = GetDragPositionNoYClamped(cursorPosition.X, 
            _sliderBgLeftBound, 
            _sliderBgRightBound); 
        Position = newDrag;
        
        // calculate percent
        Percent = (Bounds.Left - _sliderBgLeftBound) /
                  ((_sliderBgRightBound - 1) - _sliderBgLeftBound);
        
        Console.WriteLine(Percent);
    }
}