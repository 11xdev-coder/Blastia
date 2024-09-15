using BlasterMaster.Main.UI.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI;

public class Slider : Button
{
    public override bool Draggable => true;

    private Image _backgroundImage;
    private float _bgTextureHeight;

    // flag to update background position only once
    private bool _hasUpdatedBgPosition;

    private float _sliderTextSizeX;
    private float _sliderBgLeftBound;
    private float _sliderBgRightBound;

    public float Percent { get; private set; }

    public Slider(Vector2 position, SpriteFont font) : 
        base(position, "O", font, null)
    {
        _backgroundImage = new Image(position, BlasterMasterGame.SliderTexture);
        _bgTextureHeight = _backgroundImage.Texture.Height;
        _sliderTextSizeX = Font.MeasureString(Text).X;
    }

    public override void UpdateBounds()
    {
        base.UpdateBounds();
        UpdateBackgroundImage();
    }

    private void UpdateBackgroundImage()
    {
        if (_backgroundImage != null && !_hasUpdatedBgPosition)
        {
            Vector2 position = new Vector2(Bounds.Left + _sliderTextSizeX * 0.5f, 
                Bounds.Center.Y - _bgTextureHeight * 0.65f);
            _backgroundImage.Position = position;
            _backgroundImage.UpdateBounds();

            _sliderBgLeftBound = _backgroundImage.Bounds.Left - _sliderTextSizeX * 0.5f;
            _sliderBgRightBound = _backgroundImage.Bounds.Right - _sliderTextSizeX * 0.5f;
            
            _hasUpdatedBgPosition = true;
        }
    }

    public void AddToElements(List<UIElement> elements)
    {
        elements.Add(_backgroundImage);
        elements.Add(this);
    }

    protected override void UpdateDragPosition(Vector2 cursorPosition)
    {
        Vector2 newDrag = GetDragPositionNoYClamped(cursorPosition.X, 
            _sliderBgLeftBound, 
            _sliderBgRightBound); 
        Position = newDrag;

        Percent = (Bounds.Left - _sliderBgLeftBound) /
                  ((_sliderBgRightBound - 1) - _sliderBgLeftBound);
        
        Console.WriteLine(Percent);
    }
}