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

    private bool _isFocused;
    
    private double BlinkTimer { get; set; }
    private Color CursorColor { get; set; }
    private int CursorWidth { get; set; }
    private int CursorHeight { get; set; }

    private double _leftArrowHoldTime;
    private double _rightArrowHoldTime;
    private const double InitialHoldDelay = 0.5f;
    private const double HoldRepeatInterval = 0.1f;

    private Color _defaultTextColor = Color.Wheat;
    private Color _normalTextColor = Color.White;
    
    public Input(Vector2 position, SpriteFont font, bool cursorVisible = false,
        double blinkInterval = 0.15f, Color? cursorColor = default, bool focusedByDefault = false,
        int cursorWidth = 2, int cursorHeight = 30) : base(position, "", font)
    {
        _cursorVisible = cursorVisible;
        _shouldDrawCursor = cursorVisible;
        _blinkInterval = blinkInterval;

        CursorColor = cursorColor ?? Color.White;
        CursorWidth = cursorWidth;
        CursorHeight = cursorHeight;
        
        _isFocused = focusedByDefault;
        OnClick += Focus;
    }

    public override void Update()
    {
        base.Update();
        
        // if clicked not on this element -> unfocus
        if (BlasterMasterGame.HasClickedLeft && !IsHovered)
        {
            Unfocus();
        }
        
        // handle input if focused
        if (_isFocused)
        {
            HandleArrows();
            KeyboardHelper.ProcessInput(ref _cursorIndex, _stringBuilder);
        }
        
        // if no text + unfocused -> default text; otherwise -> StringBuilder text
        if (!_isFocused && _stringBuilder.Length <= 0)
        {
            Text = "Text here...";
            DrawColor = _defaultTextColor;
        }
        else
        {
            Text = _stringBuilder.ToString();
            DrawColor = _normalTextColor;
        }
        
        // blink if cursor is visible + focused
        if (_cursorVisible && _isFocused) Blink();
    }

    private void HandleArrows()
    {
        // left arrow
        KeyboardHelper.ProcessKeyHold(Keys.Left, InitialHoldDelay, HoldRepeatInterval,
            ref _leftArrowHoldTime, ref _rightArrowHoldTime, LeftArrowPress);
        
        // right arrow
        KeyboardHelper.ProcessKeyHold(Keys.Right, InitialHoldDelay, HoldRepeatInterval,
            ref _rightArrowHoldTime, ref _leftArrowHoldTime, RightArrowPress);
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

    private void Focus()
    {
        _isFocused = true;
    }

    private void Unfocus()
    {
        _isFocused = false;
    }

    public override void OnMenuInactive()
    {
        _cursorIndex = 0;
        _stringBuilder.Clear();
        _shouldDrawCursor = false; // no blink in next draw
        _isFocused = false; // unfocus
        Update(); // no text in next draw
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);

        if (Font == null || Text == null) return;
        
        // draw cursor only if we should + focused
        if (_shouldDrawCursor && _isFocused)
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