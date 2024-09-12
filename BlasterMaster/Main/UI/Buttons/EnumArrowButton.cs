using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI.Buttons;

public class EnumArrowButton : Button
{
    public Button? LeftButton;
    public Button? RightButton;

    private Vector2 _leftArrowPosition;
    private Vector2 _rightArrowPosition;
    private float _arrowSpacing;

    private float _leftArrowSizeY;
    private float _rightArrowSizeX;
    
    public EnumArrowButton(Vector2 position, string text, SpriteFont font, Action onClick, 
        float arrowSpacing) : base(position, text, font, onClick)
    {
        _arrowSpacing = arrowSpacing;
        LeftButton = new Button(_leftArrowPosition, "<", font, OnLeftArrowClick);
        RightButton = new Button(_rightArrowPosition, ">", font, OnRightArrowClick);
        
        _leftArrowSizeY = font.MeasureString(LeftButton.Text).Y;
        _rightArrowSizeX = font.MeasureString(RightButton.Text).Y;
        UpdateArrowPositions();
    }

    public override void UpdateBounds()
    {
        base.UpdateBounds();
        UpdateArrowPositions();
    }

    /// <summary>
    /// Adds LeftArrow, this button and RightArrow to the elements list
    /// </summary>
    /// <param name="elements"></param>
    public void AddToElements(List<UIElement> elements)
    {
        if (LeftButton != null) elements.Add(LeftButton);
        elements.Add(this);
        if (RightButton != null) elements.Add(RightButton);
    }

    private void UpdateArrowPositions()
    {
        _leftArrowPosition = new Vector2(Bounds.Left - _arrowSpacing * 2, Bounds.Center.Y - _leftArrowSizeY / 2);
        if (LeftButton != null)
        {
            LeftButton.Position = _leftArrowPosition;
            LeftButton.UpdateBounds();
        }
        
        _rightArrowPosition = new Vector2(Bounds.Right + _arrowSpacing, Bounds.Center.Y - _rightArrowSizeX / 2);
        if (RightButton != null)
        {
            RightButton.Position = _rightArrowPosition;
            RightButton.UpdateBounds();
        }
    }

    private void OnLeftArrowClick()
    {
        
    }

    private void OnRightArrowClick()
    {
        
    }
}