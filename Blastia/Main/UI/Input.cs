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
    
    /// <summary>
    /// Allows multi-line input and doesn't try to center this element (keeps in one place)
    /// </summary>
    public bool IsSignEditing { get; set; }
    public int CharacterLimit { get; set; } = 280;
    /// <summary>
    /// Horizontal line size which when exceeded will start a new line
    /// </summary>
    public float WrapTextSize { get; set; } = 650;
    /// <summary>
    /// If true, when <c>WrapTextSize</c> is exceeded instead of wrapping to the new line will start moving this element to the left. Only works when <c>IsSignEditing</c> is true
    /// </summary>
    public bool MoveInsteadOfWrapping { get; set; }
    /// <summary>
    /// Used when <c>MoveInsteadOfWrapping</c> is true for better horizontal cursor scrolling
    /// </summary>
    private int _cachedDisplayStart;
    /// <summary>
    /// Used when <c>MoveInsteadOfWrapping</c> is true. If this is true, will recalculate the start of display text (visible part not whole text)
    /// </summary>
    private bool _shouldRecalcOptimalStart;

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
    
    /// <summary>
    /// Calculates maximum possible bounds depending on max text size
    /// </summary>
    /// <returns></returns>
    private Rectangle GetMaxPossibleBounds() 
    {
        if (Font == null || !IsSignEditing) return Rectangle.Empty;
        
        // horizontal only
        if (MoveInsteadOfWrapping) 
        {
            var height = Font.LineSpacing;
            return new Rectangle((int) Position.X, (int) Position.Y,
                (int) WrapTextSize, height);
        }
        else 
        {
            // calculate multiple lines
            var lines = GetMaxPossibleLines();
            var height = Font.LineSpacing * lines;
            
            return new Rectangle((int) Position.X, (int) Position.Y, (int) WrapTextSize, height);
        }
    }
    
    /// <summary>
    /// Estimates average amount of lines (using 'M' character)
    /// </summary>
    /// <returns></returns>
    private int GetMaxPossibleLines() 
    {
        if (Font == null) return 1;
        
        var avgCharWidth = Font.MeasureString("M").X;
        var charsPerLine = (int) (WrapTextSize / avgCharWidth);
        
        if (charsPerLine <= 0) return 1;
        
        var lines = (int) Math.Ceiling((double) CharacterLimit / charsPerLine);
        if (lines <= 0) return 1;
        return lines;
    }

    public override void Update()
    {
        base.Update();

        if (Font == null) return;

        if (IsSignEditing)
        {
            // clamp cursor index to valid range
            _cursorIndex = Math.Clamp(_cursorIndex, 0, StringBuilder.Length);
        }

        var previousLength = StringBuilder.Length;
        var previousCursorIndex = _cursorIndex;
        
        // Ctrl V first
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
                var currentWidth = 0f;
                for (int i = 0; i < plain.Length; i++)
                {
                    var symbolSize = Font.MeasureString(plain[i].ToString());
                    
                    if (currentWidth + symbolSize.X >= WrapTextSize - symbolSize.X - 10 && !MoveInsteadOfWrapping) 
                    {
                        wrapped.Append('\n');
                        currentWidth = 0f;
                    }

                    wrapped.Append(plain[i]);
                    currentWidth += symbolSize.X;
                }
                Text = wrapped.ToString();
                DrawColor = _normalTextColor;
            }

            // optimal start recalc
            var safeText = GetSafeText();
            if (MoveInsteadOfWrapping && Font.MeasureString(safeText).X >= WrapTextSize) 
            {
                var charsAdded = StringBuilder.Length - previousLength;
                if (charsAdded > 0 && previousLength == previousCursorIndex) // added text at the end
                {
                    // just recalculate start
                    _shouldRecalcOptimalStart = true;
                }
                else if (charsAdded > 0) // in the middle + exceed wrap size
                {
                    // if cursor exceeds display text length
                    var displayText = GetDisplayText(safeText, _cachedDisplayStart);
                    var cursorRelativeToStart = _cursorIndex - _cachedDisplayStart;
                    
                    if (cursorRelativeToStart > displayText.Length)
                    {
                        // how many characters dont fit
                        var charactersOverflow = cursorRelativeToStart - displayText.Length;
                        _cachedDisplayStart += charactersOverflow;
                    }
                }
            }
        }
        else
        {
            var setDefaultText = !IsFocused && StringBuilder.Length <= 0;
            Text = setDefaultText ? DefaultText : StringBuilder.ToString();
            DrawColor = setDefaultText ? _defaultTextColor : _normalTextColor;
        }

        // blink if cursor is visible + focused
        if (_cursorVisible && IsFocused) Blink();

        // ensure hitbox matches sign-edit area
        if (IsSignEditing)
        {
            UpdateBounds();
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
                filtered = filtered.Substring(0, Math.Min(filtered.Length, maxLength)); // cut the text
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
        if (_cursorIndex < _cachedDisplayStart) _cachedDisplayStart = _cursorIndex;
    }
    
    private void RightArrowPress()
    {
        if (_cursorIndex < StringBuilder.Length) _cursorIndex += 1;

        var displayText = GetDisplayText(GetSafeText(), _cachedDisplayStart);
        if (_cursorIndex > displayText.Length) _cachedDisplayStart = _cursorIndex - displayText.Length; 
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
    
    /// <summary>
    /// using StringBuilder is more safe (Text can differ from actual user input)
    /// </summary>
    /// <returns></returns>
    private string GetSafeText() => StringBuilder.Length > 0 ? StringBuilder.ToString() : " ";
    /// <summary>
    /// Clamps cursor index to not be out of bounds
    /// </summary>
    /// <returns></returns>
    private int GetSafeCursorIndex() => Math.Clamp(_cursorIndex, 0, StringBuilder.Length);

    /// <summary>
    /// Calculates cursor position depending on <c>_cursorIndex</c> and draws it
    /// </summary>
    /// <param name="lines">Splitted text lines</param>
    /// <param name="lineHeight"></param>
    private void DrawCursor(SpriteBatch spriteBatch, int cursorIndex, string[] lines, float lineHeight) 
    {
        if (Font == null) return;
        
        // draw cursor at end of text
        if (_shouldDrawCursor && IsFocused)
        {
            int acc = 0;
            int lineIdx = 0;
            foreach (var ln in lines)
            {
                if (cursorIndex <= acc + ln.Length)
                {
                    int posInLine = cursorIndex - acc;
                    var substr = ln[..Math.Min(posInLine, ln.Length)];
                    var size = Font.MeasureString(substr) * Scale;
                    var cursorPos = new Vector2(Bounds.Left + size.X, Bounds.Top + lineIdx * lineHeight);
                    var rect = new Rectangle((int)cursorPos.X, (int)cursorPos.Y, CursorWidth, CursorHeight);
                    spriteBatch.Draw(BlastiaGame.TextureManager.WhitePixel(), rect, CursorColor * Alpha);
                    break;
                }
                acc += ln.Length;
                lineIdx++;
            }
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (IsSignEditing)
        {
            if (Font == null || Text == null) return;
            
            var lines = Text.Split('\n');
            float lineHeight = Font.LineSpacing * Scale.Y;
            
            // imitate moving text to the left instead of starting new line
            if (MoveInsteadOfWrapping) 
            {
                var safeText = GetSafeText();
                var start = 0;
                
                // exceeded wrap size
                if (StringBuilder.Length > 0 && Font.MeasureString(safeText).X >= WrapTextSize) 
                {
                    // only if we need to recalc
                    if (_shouldRecalcOptimalStart) 
                    {
                        // binary serach to earliest start
                        var low = 0;
                        var high = StringBuilder.Length;
                        
                        while (low <= high) 
                        {
                            var mid = (low + high) / 2;
                            var substring = safeText.Substring(mid); // from mid to end
                            var textWidth = Font.MeasureString(substring);
                            
                            // this start works, try finding earlier one
                            if (textWidth.X <= WrapTextSize) 
                            {  
                                start = mid;
                                high = mid - 1;                            
                            }
                            else // doesnt work, try later position 
                            {
                                low = mid + 1;
                            }
                        }

                        _cachedDisplayStart = start; // update cached (but only when recalculating)
                        _shouldRecalcOptimalStart = false;
                    }
                    else 
                    {
                        // dont need recalc
                        start = _cachedDisplayStart; // else use cached value (when text is typed in the middle)
                    }
                }
                else 
                {
                    // entire text fits
                    start = 0;
                    _cachedDisplayStart = 0;
                }

                // get cursor index relative to this substring
                var displayText = GetDisplayText(safeText, start);;
                var displayLines = displayText.Split('\n');
                var cursorIndex = Math.Clamp(_cursorIndex - start, 0, displayText.Length);
                
                var pos = new Vector2(Bounds.Left, Bounds.Top);
                base.Draw(spriteBatch, pos, displayText); // draw text
                DrawCursor(spriteBatch, cursorIndex, displayLines, lineHeight); // draw cursor before exiting
                return;
            }
            
            
            // draw each line left-aligned
            for (int i = 0; i < lines.Length; i++)
            {
                var pos = new Vector2(Bounds.Left, Bounds.Top + i * lineHeight);
                base.Draw(spriteBatch, pos, lines[i]);
            }
            DrawCursor(spriteBatch, GetSafeCursorIndex(), lines, lineHeight);
        }
        else
        {
            base.Draw(spriteBatch);
            if (Font == null || Text == null) return;
            if (_shouldDrawCursor && IsFocused)
            {
                int safeCursorIndex = GetSafeCursorIndex();
                var safeText = GetSafeText();
                
                float yOffset = string.IsNullOrEmpty(Text) ? 10 : 0;
                var textSize = Font.MeasureString(safeText[..safeCursorIndex]);
                var cursorPosition = new Vector2(Bounds.Left + textSize.X, Bounds.Center.Y - CursorHeight * 0.5f - yOffset);
                var cursorRectangle = new Rectangle((int)cursorPosition.X, (int)cursorPosition.Y, CursorWidth, CursorHeight);
                spriteBatch.Draw(BlastiaGame.TextureManager.WhitePixel(), cursorRectangle, CursorColor * Alpha);
            }
        }
    }

    /// <summary>
    /// Returns a substring from <c>originalText</c> starting from <c>start</c> that fits into <c>WrapTextSize</c>
    /// </summary>
    private string GetDisplayText(string originalText, int start)
    {
        if (Font == null) return "";
        
        for (int i = start + 1; i <= originalText.Length; i++) 
        {
            var substring = originalText.Substring(start, i - start);
            var size = Font.MeasureString(substring);
            
            if (size.X > WrapTextSize) 
            {
                // dont go before start + 1
                i = Math.Max(start + 1, i - 1);
                return originalText.Substring(start, i - start);
            }
        }

        // whole text fits
        return originalText.Substring(start);
    }
    
    public override void UpdateBounds()
    {
        if (IsSignEditing && Font != null && Text != null)
        {
            if (MoveInsteadOfWrapping) 
            {
                // single line mode -> width is wrap length and height is 1 line
                // entered text width or wrap size
                var width = Math.Min(WrapTextSize, Font.MeasureString(GetSafeText()).X);
                var height = Font.LineSpacing;
                UpdateBoundsBase(width, height);
            }
            else 
            {
                // empty string -> 1f size
                if (string.IsNullOrEmpty(Text)) 
                {
                    UpdateBoundsBase(1f, 1f);
                    return;
                }
                
                // get max possible width out of all lines
                var lines = Text.Split('\n');
                var maxWidth = 0f;
                
                foreach (var line in lines) 
                {
                    var lineWidth = Font.MeasureString(line);
                    maxWidth = Math.Max(maxWidth, lineWidth.X);
                }

                float height = Font.LineSpacing * lines.Length;

                // apply alignment
                UpdateBoundsBase(maxWidth, height);
            }           
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