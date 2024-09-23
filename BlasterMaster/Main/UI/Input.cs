using System.Text;
using BlasterMaster.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BlasterMaster.Main.UI;

public class Input : UIElement
{
    private StringBuilder _stringBuilder = new();

    private readonly double _blinkInterval;
    private readonly bool _cursorVisible;
    private bool _shouldDrawCursor;

    private int _cursorIndex;

    private double BlinkTimer { get; set; }
    private Color CursorColor { get; set; }
    private int CursorWidth { get; set; }
    private int CursorHeight { get; set; }

    private double _leftArrowHoldTime;
    private double _rightArrowHoldTime;
    private const double InitialHoldDelay = 0.5f;
    private const double HoldRepeatInterval = 0.1f;
    
    public Input(Vector2 position, SpriteFont font, bool cursorVisible = false,
        double blinkInterval = 0.15f, Color? cursorColor = default, 
        int cursorWidth = 2, int cursorHeight = 30) : base(position, "", font)
    {
        _cursorVisible = cursorVisible;
        _shouldDrawCursor = cursorVisible;
        _blinkInterval = blinkInterval;

        CursorColor = cursorColor ?? Color.White;
        CursorWidth = cursorWidth;
        CursorHeight = cursorHeight;
    }

    public override void Update()
    {
        base.Update();
        
        HandleArrows();
        
        // TODO: delete + hold
        KeyboardHelper.ProcessInput(BlasterMasterGame.KeyboardState, BlasterMasterGame.PreviousKeyboardState,
            _stringBuilder);
        
        Text = _stringBuilder.ToString();

        if (_cursorVisible) Blink();
    }

    private void HandleArrows()
    {
        // left arrow
        HandleArrow(Keys.Left, ref _leftArrowHoldTime, ref _rightArrowHoldTime, LeftArrowPress);
        
        // right arrow
        HandleArrow(Keys.Right, ref _rightArrowHoldTime, ref _leftArrowHoldTime, RightArrowPress);
    }

    /// <summary>
    /// Handles the logic for an arrow key press, including handling hold and repeat actions.
    /// </summary>
    /// <param name="arrowKey">The arrow key being pressed (left or right).</param>
    /// <param name="arrowKeyHoldTime">The reference to the time the arrow key has been held down.</param>
    /// <param name="oppositeArrowKeyHoldTime">The reference to the hold time of the opposite arrow key, which is reset if the current arrow key is pressed.</param>
    /// <param name="pressAction">The action to execute when the arrow key is pressed.</param>
    private void HandleArrow(Keys arrowKey, ref double arrowKeyHoldTime, 
        ref double oppositeArrowKeyHoldTime, Action pressAction)
    {
        if (BlasterMasterGame.KeyboardState.IsKeyDown(arrowKey))
        {
            oppositeArrowKeyHoldTime = 0;
            
            // single press
            if (BlasterMasterGame.PreviousKeyboardState.IsKeyUp(arrowKey))
            {
                pressAction();
                arrowKeyHoldTime = 0;
            }
            else
            {
                // still holding
                arrowKeyHoldTime += BlasterMasterGame.GameTimeElapsedSeconds;
                if (arrowKeyHoldTime >= InitialHoldDelay)
                {
                    arrowKeyHoldTime -= HoldRepeatInterval;
                    pressAction();
                }
            }
        }
        else
        {
            // reset if not pressed
            arrowKeyHoldTime = 0;
        }
    }

    private void LeftArrowPress()
    {
        if (_cursorIndex > 0) _cursorIndex -= 1;
    }
    
    private void RightArrowPress()
    {
        if (_cursorIndex < _stringBuilder.Length) _cursorIndex += 1;
    }

    private void Blink()
    {
        BlinkTimer += BlasterMasterGame.GameTimeElapsedSeconds;
        if (BlinkTimer >= _blinkInterval)
        {
            _shouldDrawCursor = !_shouldDrawCursor;
            BlinkTimer -= _blinkInterval;
        }
    }

    public override void OnMenuInactive()
    {
        _cursorIndex = 0;
        _stringBuilder.Clear();
        _shouldDrawCursor = false; // no blink in next draw
        Update(); // no text in next draw
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);

        if (_shouldDrawCursor)
        {
            // little offset if no text
            float yOffset = 0;
            if (string.IsNullOrEmpty(Text)) yOffset = 10;
            
            // measure text size until _cursorIndex
            var textSizeToCursorIndex = 
                Font.MeasureString(Text.Substring(0, _cursorIndex));
            
            var cursorPosition = new Vector2(Bounds.Left + textSizeToCursorIndex.X, 
                Bounds.Center.Y - CursorHeight * 0.5f - yOffset);
            
            var cursorRectangle = new Rectangle((int)cursorPosition.X, (int)cursorPosition.Y, 
                CursorWidth, CursorHeight);
            spriteBatch.Draw(BlasterMasterGame.WhitePixel, cursorRectangle, CursorColor);
        }
    }
}