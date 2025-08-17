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
    
    public bool IsSignEditing { get; set; }
    public int CharacterLimit { get; set; } = 144;
    public int WrapLength { get; set; } = 36;

    private bool _ctrlVPressedLastFrame;

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

        if (IsSignEditing)
        {
            // clamp cursor index to valid range
            _cursorIndex = Math.Clamp(_cursorIndex, 0, StringBuilder.Length);
        }

        // handle input if focused
        if (IsFocused)
        {
            HandleArrows();
            try
            {
                KeyboardHelper.ProcessInput(ref _cursorIndex, StringBuilder);
            }
            catch (IndexOutOfRangeException)
            {
                _cursorIndex = StringBuilder.Length;
            }
        }
        
        if (IsSignEditing)
        {
            // clamp length and cursor
            if (StringBuilder.Length > CharacterLimit)
            {
                StringBuilder.Length = CharacterLimit;
                _cursorIndex = Math.Min(_cursorIndex, StringBuilder.Length);
            }
            // placeholder when not editing or empty
            if (!IsFocused && StringBuilder.Length <= 0)
            {
                Text = DefaultText;
                DrawColor = _defaultTextColor;
            }
            else
            {
                // wrap text
                var plain = StringBuilder.ToString();
                var wrapped = new StringBuilder();
                for (int i = 0; i < plain.Length; i += WrapLength)
                {
                    int len = Math.Min(WrapLength, plain.Length - i);
                    wrapped.Append(plain.Substring(i, len));
                    if (i + len < plain.Length) wrapped.Append('\n');
                }
                Text = wrapped.ToString();
                DrawColor = _normalTextColor;
            }
        }
        else
        {
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
        }

        // blink if cursor is visible + focused
        if (_cursorVisible && IsFocused) Blink();

        // ensure hitbox matches sign-edit area
        if (IsSignEditing)
        {
            UpdateBounds();
        }
        
        // Ctrl V
		var ctrlPressed = BlastiaGame.KeyboardState.IsKeyDown(Keys.LeftControl) || BlastiaGame.KeyboardState.IsKeyDown(Keys.RightControl);
		var vPressed = BlastiaGame.KeyboardState.IsKeyDown(Keys.V);
		if (ctrlPressed && vPressed) 
		{
			if (!_ctrlVPressedLastFrame) 
			{
			    _ctrlVPressedLastFrame = true;
                PasteText();
			}
		}
		else 
		{
			_ctrlVPressedLastFrame = false;
		}
    }
    
    private void PasteText() 
    {
        var clipboardText = TextCopy.ClipboardService.GetText();
        if (string.IsNullOrEmpty(clipboardText)) return;
        var filtered = FilterText(clipboardText);
        
        if (StringBuilder.Length + filtered.Length > CharacterLimit) 
        {
            // cut to fit the limit
            int maxLength = CharacterLimit - StringBuilder.Length; // space to fit in
            if (maxLength > 0)
                filtered = clipboardText.Substring(0, Math.Min(filtered.Length, maxLength)); // cut the text
            else
                return; // no space left
        }

        // paste
        StringBuilder.Insert(_cursorIndex, filtered);
        _cursorIndex += filtered.Length;
    }
    
    private string FilterText(string input) 
    {
        var filtered = new StringBuilder();
        foreach (var c in input) 
        {
            // character that performs an action (like \n or \r)
            if (char.IsControl(c))
            {
                if (c == '\n' || c == '\r')
                    filtered.Append(IsSignEditing ? '\n' : ' ');
            }
            else
                filtered.Append(c);
        }

        return filtered.ToString();
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
        if (IsSignEditing)
        {
            if (Font == null || Text == null) return;
            var lines = Text.Split('\n');
            float lineHeight = Font.LineSpacing * Scale.Y;
            // draw each line left-aligned
            for (int i = 0; i < lines.Length; i++)
            {
                var pos = new Vector2(Bounds.Left, Bounds.Top + i * lineHeight);
                spriteBatch.DrawString(Font, lines[i], pos, DrawColor * Alpha, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
            }
            // draw cursor at end of text
            if (_shouldDrawCursor && IsFocused)
            {
                // calculate cursor position in wrapped text
                int safeCursorIndex = Math.Clamp(_cursorIndex, 0, StringBuilder.Length);
                int acc = 0;
                int lineIdx = 0;
                foreach (var ln in lines)
                {
                    if (safeCursorIndex <= acc + ln.Length)
                    {
                        int posInLine = safeCursorIndex - acc;
                        var substr = ln[..Math.Min(posInLine, ln.Length)];
                        var size = Font.MeasureString(substr) * Scale;
                        var cursorPos = new Vector2(Bounds.Left + size.X, Bounds.Top + lineIdx * lineHeight);
                        var rect = new Rectangle((int)cursorPos.X, (int)cursorPos.Y, CursorWidth, CursorHeight);
                        spriteBatch.Draw(BlastiaGame.WhitePixel, rect, CursorColor * Alpha);
                        break;
                    }
                    acc += ln.Length;
                    lineIdx++;
                }
            }
        }
        else
        {
            base.Draw(spriteBatch);
            if (Font == null || Text == null) return;
            if (_shouldDrawCursor && IsFocused)
            {
                int safeCursorIndex = Math.Clamp(_cursorIndex, 0, StringBuilder.Length);
                var safeText = StringBuilder.Length > 0 ? StringBuilder.ToString() : " "; // use StringBuilder content
                
                float yOffset = string.IsNullOrEmpty(Text) ? 10 : 0;
                var textSize = Font.MeasureString(safeText[..safeCursorIndex]);
                var cursorPosition = new Vector2(Bounds.Left + textSize.X, Bounds.Center.Y - CursorHeight * 0.5f - yOffset);
                var cursorRectangle = new Rectangle((int)cursorPosition.X, (int)cursorPosition.Y, CursorWidth, CursorHeight);
                spriteBatch.Draw(BlastiaGame.WhitePixel, cursorRectangle, CursorColor * Alpha);
            }
        }
    }

    public override void UpdateBounds()
    {
        if (IsSignEditing && Font != null)
        {
            // fixed hitbox width based on wrap length, supports placeholder click
            var charSize = Font.MeasureString("W") * Scale;
            float width = charSize.X * WrapLength;
            float lineHeight = Font.LineSpacing * Scale.Y;
            int maxLines = (CharacterLimit + WrapLength - 1) / WrapLength;
            float height = lineHeight * maxLines;

            // apply alignment
            UpdateBoundsBase(width, height);
        }
        else
        {
            base.UpdateBounds();
        }
    }

    /// <summary>
    /// Set input text, replacing any existing content
    /// </summary>
    public void SetText(string newText)
    {
        StringBuilder.Clear();
        StringBuilder.Append(newText);
        _cursorIndex = StringBuilder.Length;
        Text = newText;
    }
}