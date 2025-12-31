using Blastia.Main.UI.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI;

public class SliderHandle(Vector2 position, string text, SpriteFont font, Slider parentSlider, Action updateDragAction)
    : Button(position, text, font, null)
{
    public override bool Draggable => true;
    public override bool AffectedByAlignOffset => false;

    private readonly Vector2 _fontSize = font.MeasureString(text);

    public float CalculateSliderLeftBound() => parentSlider.Bounds.Left - _fontSize.X * 0.5f;
    public float CalculateSliderRightBound() => parentSlider.Bounds.Right - _fontSize.X * 0.5f;
    private float CalculateSliderVertical() => parentSlider.Bounds.Top - _fontSize.Y * 0.25f;
    
    protected override void UpdateDragPosition(Vector2 cursorPosition)
    {
        // get drag X position (clamped to the BG)
        var newDrag = GetDragPositionNoYClamped(cursorPosition.X,
            CalculateSliderLeftBound(), CalculateSliderRightBound()); 
        Position = new Vector2(newDrag.X, CalculateSliderVertical());
        
        updateDragAction.Invoke();
    }

    public override void Update()
    {
        var rightBound = CalculateSliderRightBound();
        var leftBound = CalculateSliderLeftBound();
        var newX = leftBound + parentSlider.Percent * (rightBound - leftBound);
        var alignedX = GetAlignedPositionX(newX);
        
        Position = new Vector2(alignedX, CalculateSliderVertical());
        
        base.Update();
    }
}