using System.Text;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Blastia.Main.UI;

public class Input : UIElement
{
    public StringBuilder StringBuilder = new();

    private readonly double _blinkInterval;
    private readonly bool _cursorVisible;
    private bool _shouldDrawCursor;

    private int _cursorIndex;
    
    private double BlinkTimer { get; set; }
    private Color CursorColor { get; set; }
    private int CursorWidth { get; set; }
    private int CursorHeight { get; set; }
    private string DefaultText { get; set; }

    private double _leftArrowHoldTime;
    private double _rightArrowHoldTime;
    private const double InitialHoldDelay = 0.5f;
    private const double HoldRepeatInterval = 0.1f;

    private Color _defaultTextColor = Color.Wheat;
    private Color _normalTextColor = Color.White;
    
    public Input(Vector2 position, SpriteFont font, bool cursorVisible = false,
        double blinkInterval = 0.15f, Color? cursorColor = default, bool focusedByDefault = false,
        int cursorWidth = 2, int cursorHeight = 30, string defaultText = "Text here...") : base(position, "", font)
    {
        _cursorVisible = cursorVisible;
        _shouldDrawCursor = cursorVisible;
        _blinkInterval = blinkInterval;

        CursorColor = cursorColor ?? Color.White;
        CursorWidth = cursorWidth;
        CursorHeight = cursorHeight;
        DefaultText = defaultText;
        
        IsFocused = focusedByDefault;
    }

    public override void Update()
    {
        base.Update();
        
        // handle input if focused
        if (IsFocused)
        {
            HandleArrows();
            KeyboardHelper.ProcessInput(ref _cursorIndex, StringBuilder);
        }
        
        // if no text + unfocused -> default text; otherwise -> StringBuilder text
        if (!IsFocused && StringBuilder.Length <= 0)
        {
            Text = DefaultText;
            DrawColor = _defaultTextColor;
        }
        else
        {
            Text = StringBuilder.ToString();
            DrawColor = _normalTextColor;
        }
        
        // blink if cursor is visible + focused
        if (_cursorVisible && IsFocused) Blink();
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
        if (_cursorIndex < StringBuilder.Length) _cursorIndex += 1;
    }

    private void Blink()
    {
        BlinkTimer += BlastiaGame.GameTimeElapsedSeconds;
        if (BlinkTimer >= _blinkInterval)
        {
            _shouldDrawCursor = !_shouldDrawCursor;
            BlinkTimer -= _blinkInterval;
        }
    }

    public override void OnMenuInactive()
    {
        _cursorIndex = 0;
        StringBuilder.Clear();
        _shouldDrawCursor = false; // no blink in next draw
        IsFocused = false; // unfocus
        Update(); // no text in next draw
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);

        if (Font == null || Text == null) return;
        
        // draw cursor only if we should + focused
        if (_shouldDrawCursor && IsFocused)
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
            spriteBatch.Draw(BlastiaGame.WhitePixel, cursorRectangle, CursorColor);
        }
    }
}