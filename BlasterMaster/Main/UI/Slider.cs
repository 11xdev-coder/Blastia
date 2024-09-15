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
            _backgroundImage.Bounds.Left - _sliderTextSizeX * 0.5f, 
            _backgroundImage.Bounds.Right - _sliderTextSizeX * 0.5f); 
        Position = newDrag;
    }
}