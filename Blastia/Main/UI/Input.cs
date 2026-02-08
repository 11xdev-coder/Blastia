using System.Reflection.Emit;
using System.Text;
using Assimp;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Blastia.Main.UI;

public enum InputMode 
{
    SingleLine,
    MultipleLines,
    ScrollHorizontally
}

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
    
    public int CharacterLimit { get; set; } = 280;
    /// <summary>
    /// Horizontal line size which when exceeded will start a new line
    /// </summary>
    public float WrapTextSize { get; set; } = 650;
    /// <summary>
    /// <para><c>SingleLine</c> -> only 1 line</para>
    /// <para><c>MultipleLines</c> ->multiple lines, but text wraps to new line automatically after <c>WrapTextSize</c> is reached</para>
    /// <para><c>ScrollHorizontally</c> -> 1 line, but after reaching <c>WrapTextSize</c> the text begins to scroll to the left</para>
    /// </summary>
    public InputMode Mode { get; set; } = InputMode.SingleLine;
    /// <summary>
    /// If <c>MoveInsteadOfWrapping</c> is true -> indicates character index from where we start showing text
    /// </summary>
    private int _cachedDisplayStart;

    private bool _ctrlVPressedLastFrame;
    
    private string _labelText = "";
    private Text? _labelTextUi;
    /// <summary>
    /// Distance between the label text and input
    /// </summary>
    private const float LabelPadding = 10f;

    public Input(Vector2 position, SpriteFont font, bool cursorVisible = false,
        double blinkInterval = 0.15f, Color? cursorColor = default, bool focusedByDefault = false,
        int cursorWidth = 2, int cursorHeight = 30, string defaultText = "Text here...", string labelText = "") : base(position, "", font)
    {
        _cursorVisible = cursorVisible;
        _shouldDrawCursor = cursorVisible;
        _blinkInterval = blinkInterval;

        CursorColor = cursorColor ?? Color.White;
        CursorWidth = cursorWidth;
        CursorHeight = cursorHeight;
        DefaultText = defaultText;
        
        IsFocused = focusedByDefault;
        
        if (!string.IsNullOrEmpty(labelText)) 
        {
            _labelText = labelText;
            _labelTextUi = new Text(GetLabelTextPosition(), _labelText, font);
        }
        
        OnStartHovering += Select;
        OnEndHovering += Deselect;
    }
    
    private void Select() => Background?.SetBorderColor(Color.Yellow);
    private void Deselect() => Background?.SetBorderColor(OriginalBorderColor);
    
    /// <summary>
    /// Calculates proper bounds for the background creation. Covers both label and the input
    /// </summary>
    /// <returns></returns>
    public Rectangle GetBackgroundBounds() 
    {
        if (Font == null) return Rectangle.Empty;
        
        var oneLineHeight = Font.LineSpacing;
        var offset = GetLabelOffset();
        
        // multiple line input
        switch (Mode) 
        {
            case InputMode.MultipleLines:
            {
                var lines = GetMaxPossibleLines();
                var inputHeight = oneLineHeight * lines;
                return new Rectangle(
                    (int)Position.X, 
                    (int)(Position.Y - offset.Y),
                    (int)WrapTextSize, 
                    (int)(inputHeight + offset.Y)
                );
            }
            case InputMode.ScrollHorizontally:
            {
                // text scrolls horizontally after WrapTextSize
                return new Rectangle(
                    (int)(Position.X - offset.X), 
                    (int)Position.Y,
                    (int)(WrapTextSize + offset.X), 
                    oneLineHeight
                );
            }
            case InputMode.SingleLine:
            default:
            {
                // text doesnt scroll, only limited by CharacterLimit 
                var avgCharWidth = Font.MeasureString("M").X;
                var width = CharacterLimit * avgCharWidth;
                
                return new Rectangle(
                    (int)(Position.X - offset.X), 
                    (int)Position.Y,
                    (int)(width + offset.X), 
                    oneLineHeight
                );
            }
        }
    }
    
    /// <summary>
    /// Returns the offset for the label text
    /// </summary>
    /// <returns></returns>
    private Vector2 GetLabelOffset() 
    {
        if (string.IsNullOrEmpty(_labelText) || Font == null) return Vector2.Zero;
        
        switch (Mode) 
        {
            case InputMode.MultipleLines:
            {
                return new Vector2(0f, Font.MeasureString(_labelText).Y + LabelPadding);
            }
            case InputMode.ScrollHorizontally:
            case InputMode.SingleLine:
            default:
            {
                return new Vector2(Font.MeasureString(_labelText).X + LabelPadding, 0f);
            }
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
    
    /// <summary>
    /// Pastes the text if Ctrl and V were pressed
    /// </summary>
    private void HandleCtrlVPaste() 
    {
		var ctrlPressed = BlastiaGame.KeyboardState.IsKeyDown(Keys.LeftControl) || BlastiaGame.KeyboardState.IsKeyDown(Keys.RightControl);
		var vPressed = BlastiaGame.KeyboardState.IsKeyDown(Keys.V);
		var ctrlVPressed = ctrlPressed && vPressed;
		
		if (ctrlVPressed && !_ctrlVPressedLastFrame) 
		{
            PasteText();
		}
		_ctrlVPressedLastFrame = ctrlVPressed;
    }
    
    /// <summary>
    /// Processes keyboard input, only if focused
    /// </summary>
    private void HandleInput() 
    {
        if (!IsFocused) return;
        
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
    
    /// <summary>
    /// Doesn't let the cursor and string builder go beyond character limit
    /// </summary>
    private void EnforceCharacterLimit() 
    {
        // over character limit
        if (StringBuilder.Length > CharacterLimit)
        {
            StringBuilder.Length = CharacterLimit;
            _cursorIndex = Math.Min(_cursorIndex, StringBuilder.Length);
        }
    }
    
    /// <summary>
    /// Sets the default text when there are no text and not focused
    /// </summary>
    private void SetDefaultTextIfEmpty() 
    {
        var setDefaultText = !IsFocused && StringBuilder.Length <= 0;
        Text = setDefaultText ? DefaultText : StringBuilder.ToString();
        DrawColor = setDefaultText ? _defaultTextColor : _normalTextColor;
    }
    
    /// <summary>
    /// Wraps the <c>input</c> so that each line will fit in <c>maxWidthPerLine</c>
    /// </summary>
    /// <param name="input">Original text</param>
    /// <param name="maxWidthPerLine">Maximum possible width of each line</param>
    /// <returns></returns>
    private string WrapText(string input, float maxWidthPerLine) 
    {
        if (Font == null) return input;
        
        var wrapped = new StringBuilder();
        var currentWidth = 0f;
        foreach (var c in input) 
        {
            var charWidth = Font.MeasureString(c.ToString()).X;
            
            if (currentWidth + charWidth >= maxWidthPerLine) 
            {
                wrapped.Append('\n');
                currentWidth = 0f;
            }
            
            wrapped.Append(c);
            currentWidth += charWidth;
        }
        
        return wrapped.ToString();
    }
    
    /// <summary>
    /// Update designed for multiple line mode, only if text is focused or is not empty
    /// </summary>
    private void UpdateMultipleLineMode() 
    {
        if (!IsFocused && StringBuilder.Length <= 0) return;
        
        Text = WrapText(StringBuilder.ToString(), WrapTextSize);
        DrawColor = _normalTextColor;
    }

    /// <summary>
    /// Update designed for ScrollHorizontally mode
    /// </summary>
    /// <param name="previousLength"></param>
    /// <param name="previousCursorIndex"></param>
    private void UpdateScrollHorizontallyMode(int previousLength, int previousCursorIndex) 
    {
        if (Font == null) return;
        
        var safeText = GetSafeText();
        if (Font.MeasureString(safeText).X >= WrapTextSize) // text exceeds wrap size
        {
            UpdateHorizontalWrapPosition(safeText, previousLength, previousCursorIndex);
        }
        else // text no longer exceeds wrap size
        {
            // reset
            _cachedDisplayStart = 0;
        }
    }
    
    /// <summary>
    /// Recalculates <c>_cachedDisplayStart</c> to capture the biggest possible string that will fit in WrapTextSize
    /// </summary>
    private void RecalculateOptimalDisplayStart() 
    {
        if (Font == null) return;
        
        var safeText = GetSafeText();
        
        if (StringBuilder.Length <= 0 || Font.MeasureString(safeText).X < WrapTextSize) 
        {
            // entire text fits
            _cachedDisplayStart = 0;
            return;
        }
        
        // else -> exceeds wrap text size
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
                _cachedDisplayStart = mid; // update cached
                high = mid - 1;                            
            }
            else // doesnt work, try later position 
            {
                low = mid + 1;
            }
        }
    }
    
    /// <summary>
    /// Depending on amount of chars added, does wrapping logic
    /// </summary>
    /// <param name="safeText"></param>
    /// <param name="previousLength"></param>
    /// <param name="previousCursorIndex"></param>
    private void UpdateHorizontalWrapPosition(string safeText, int previousLength, int previousCursorIndex) 
    {
        var charsAdded = StringBuilder.Length - previousLength;
    
        if (charsAdded > 0) 
        {
            HorizontalWrapWhenAddedText(safeText, previousLength, previousCursorIndex);
        }
        else if (charsAdded < 0)
        {
            HorizontalWrapWhenRemovedText();
        }
    }
    
    /// <summary>
    /// Wrapping logic when text was added
    /// </summary>
    /// <param name="safeText"></param>
    /// <param name="previousLength"></param>
    /// <param name="previousCursorIndex"></param>
    private void HorizontalWrapWhenAddedText(string safeText, int previousLength, int previousCursorIndex) 
    {
        // added text at the end
        if (previousLength == previousCursorIndex) 
        {
            // just recalculate start
            RecalculateOptimalDisplayStart();
            return;
        }
        
        // added in the middle
        // if cursor exceeds display text length
        var displayText = GetDisplayText(safeText, _cachedDisplayStart);
        // convert cursor position from full text position relative to visible part
        // e.g. _cachedDisplayStart = 10, and we typed text at _cursorIndex = 30, then relative pos would be 30 - 10 = 20
        var cursorRelativeToStart = _cursorIndex - _cachedDisplayStart;
        
        if (cursorRelativeToStart > displayText.Length)
        {
            // calculate how many characters dont fit on the visible text (overflowed)
            var charactersOverflow = cursorRelativeToStart - displayText.Length;
            // shift visible text
            _cachedDisplayStart += charactersOverflow;
        }
    }
    
    /// <summary>
    /// Wrapping logic when text was removed
    /// </summary>
    private void HorizontalWrapWhenRemovedText() 
    {
        // moved before visible start -> move to the left
        if (_cursorIndex < _cachedDisplayStart) 
        {
            _cachedDisplayStart = _cursorIndex;                        
        }
        // recalculate if we might have space to show from earlier
        else if (_cachedDisplayStart > 0)
        {
            RecalculateOptimalDisplayStart();
        }
    }

    public override void Update()
    {
        base.Update();

        if (Font == null) return;
        
        _cursorIndex = Math.Clamp(_cursorIndex, 0, StringBuilder.Length);
        var previousLength = StringBuilder.Length;
        var previousCursorIndex = _cursorIndex;
        
        HandleCtrlVPaste();
        HandleInput();
        EnforceCharacterLimit();
        SetDefaultTextIfEmpty();
        
        switch (Mode) 
        {
            case InputMode.MultipleLines:
            {
                UpdateMultipleLineMode();
                break;
            }
            case InputMode.ScrollHorizontally:
            {
                UpdateScrollHorizontallyMode(previousLength, previousCursorIndex);
                break;
            }
        }

        if (_cursorVisible && IsFocused) Blink();
        
        _labelTextUi?.Update();
    }
    
    private void PasteText() 
    {
        var clipboardText = TextCopy.ClipboardService.GetText();
        if (string.IsNullOrEmpty(clipboardText)) return;
        
        var filtered = FilterText(clipboardText);
        if (string.IsNullOrEmpty(filtered)) return;
        
        var availableSpace = CharacterLimit - StringBuilder.Length;
        if (availableSpace <= 0) return;
        
        if (filtered.Length > availableSpace)
            filtered = filtered.Substring(0, availableSpace);
        
        // paste
        StringBuilder.Insert(_cursorIndex, filtered);
        _cursorIndex += filtered.Length;
    }
    
    private string FilterText(string input) 
    {
        var filtered = new StringBuilder();
        var allowMultipleLines = Mode == InputMode.MultipleLines;
        
        foreach (var c in input) 
        {
            if (c == '\n' || c == '\r')
            {
                filtered.Append(allowMultipleLines ? '\n' : ' ');
            }
            else if (c == '\t') 
            {
                filtered.Append("    "); // 4 spaces for tabs
            }
            else if (char.IsControl(c)) // character that performs an action (like \n or \r)
            {
                continue;
            }
            else 
            {
                filtered.Append(c);
            }                
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
    /// Find the line index and position in that line where <c>_cursorIndex</c> is located
    /// </summary>
    /// <returns></returns>
    private (int lineIndex, int positionInLine) FindCursorPosition(string[] lines, int cursorIndex)
    {
        var charsCounted = 0;
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var lastLine = i == lines.Length - 1;
            // TODO: maybe dont add +1?
            var lineLength = line.Length + (lastLine ? 0 : 1); // account for \n chars at the end (if not the last line)
            
            if (charsCounted + lineLength >= cursorIndex) // _cursorIndex between currentChars and next line
            {
                var positionInLine = cursorIndex - charsCounted;
                return (i, positionInLine);
            }
            charsCounted += lineLength;
        }
        
        return (lines.Length - 1, lines[^1].Length); // at the end
    }
    
    /// <summary>
    /// Returns a cursor rectangle
    /// </summary>
    /// <param name="lines"></param>
    /// <param name="lineIndex"></param>
    /// <param name="positionInLine"></param>
    /// <param name="lineHeight"></param>
    /// <returns></returns>
    private Rectangle GetCursorRectangle(string[] lines, int lineIndex, int positionInLine, float lineHeight) 
    {
        if (Font == null || lineIndex < 0 || lineIndex >= lines.Length) return Rectangle.Empty;
        
        var line = lines[lineIndex];
        
        var safePosition = Math.Min(positionInLine, line.Length);        
        var substr = line.Substring(0, safePosition);
        var size = Font.MeasureString(substr) * Scale;
        
        var cursorPos = new Vector2(Bounds.Left + size.X, Bounds.Top + lineIndex * lineHeight);
        var rect = new Rectangle((int)cursorPos.X, (int)cursorPos.Y, CursorWidth, CursorHeight);
        return rect;
    }
    
    /// <summary>
    /// Calculates cursor position depending on <c>_cursorIndex</c> and draws it
    /// </summary>
    private void DrawCursor(SpriteBatch spriteBatch, int cursorIndex, string[] lines, float lineHeight) 
    {
        if (!_shouldDrawCursor || !IsFocused) return;
        
        var cursorData = FindCursorPosition(lines, cursorIndex);
                
        var rect = GetCursorRectangle(lines, cursorData.lineIndex, cursorData.positionInLine, lineHeight);                   
        spriteBatch.Draw(BlastiaGame.TextureManager.WhitePixel(), rect, CursorColor * Alpha);
    }
    
    /// <summary>
    /// Draw logic for <c>MultipleLines</c> input mode
    /// </summary>
    /// <param name="spriteBatch"></param>
    /// <param name="lineHeight"></param>
    private void DrawMultipleLinesMode(SpriteBatch spriteBatch, float lineHeight) 
    {
        if (Text == null) return;
        
        var lines = Text.Split('\n');
        // draw each line left-aligned
        for (int i = 0; i < lines.Length; i++)
        {
            var pos = new Vector2(Bounds.Left, Bounds.Top + i * lineHeight);
            DrawStringAt(spriteBatch, pos, lines[i]);
        }
        DrawCursor(spriteBatch, _cursorIndex, lines, lineHeight);
    }
    
    /// <summary>
    /// Draw logic for <c>ScrolLHorizontally</c> input mode
    /// </summary>
    /// <param name="spriteBatch"></param>
    /// <param name="relativeCursorIndex">New cursor index relative to the visible part</param>
    /// <param name="displayText">The part of the text thats visible (fits in WrapTextSize)</param>
    /// <param name="lineHeight"></param>
    private void DrawScrollHorizontallyMode(SpriteBatch spriteBatch, float lineHeight) 
    {
        // get cursor index relative to this substring
        var safeText = GetSafeText();
        var displayText = GetDisplayText(safeText, _cachedDisplayStart);
        var relativeCursorIndex = Math.Clamp(_cursorIndex - _cachedDisplayStart, 0, displayText.Length);
        
        var lines = displayText.Split('\n');
        var pos = new Vector2(Bounds.Left, Bounds.Top);
        DrawStringAt(spriteBatch, pos, displayText); // draw text
        DrawCursor(spriteBatch, relativeCursorIndex, lines, lineHeight); // draw cursor before exiting
    }
    
    /// <summary>
    /// Draw logic for <c>SingleLine</c> input mode
    /// </summary>
    /// <param name="spriteBatch"></param>
    /// <param name="lineHeight"></param>
    private void DrawSingleLineMode(SpriteBatch spriteBatch, float lineHeight) 
    {
        if (Text == null) return;
        
        var pos = new Vector2(Bounds.Left, Bounds.Top);
        DrawStringAt(spriteBatch, pos, Text);
        DrawCursor(spriteBatch, _cursorIndex, [Text], lineHeight);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        Background?.Draw(spriteBatch);
        
        if (Font == null || Text == null) return;
        var lineHeight = Font.LineSpacing * Scale.Y;
        
        switch (Mode)
        {
            case InputMode.MultipleLines:
            {
                DrawMultipleLinesMode(spriteBatch, lineHeight);
                break;
            }
            case InputMode.ScrollHorizontally:
            {
                DrawScrollHorizontallyMode(spriteBatch, lineHeight);
                break;
            }
            case InputMode.SingleLine:
            {
                DrawSingleLineMode(spriteBatch, lineHeight);                
                break;
            }
        }      
        
        _labelTextUi?.Draw(spriteBatch);
    }

    /// <summary>
    /// Returns a substring from <c>originalText</c> starting from <c>start</c> that fits into <c>WrapTextSize</c>
    /// </summary>
    private string GetDisplayText(string originalText, int start)
    {
        if (Font == null) return "";
        
        // safety check
        if (start >= originalText.Length) 
        {
            Console.WriteLine($"[Input - GetDisplayText] Starting position {start} exceeds original text length {originalText.Length}");
            return "";
        }
        
        // whole text fits
        var remainingText = originalText.Substring(start);
        if (Font.MeasureString(remainingText).X <= WrapTextSize)
            return remainingText;
        
        // TODO: Optimize for binary search
        for (int length = 1; length <= originalText.Length - start; length++) 
        {
            var substring = originalText.Substring(start, length);
            var width = Font.MeasureString(substring).X;
            
            if (width > WrapTextSize) 
            {
                // return previous length (that wasnt exceeding)
                // ensure at least 1 character
                var fitLength = Math.Max(length - 1, 1);
                return originalText.Substring(start, fitLength);
            }
        }
        
        return remainingText;
    }
    
    private (float width, float height) CalculateMultipleLinesBounds() 
    {
        if (Text == null || Font == null) return (0, 0);
        
        // get max possible width out of all lines
        var lines = Text.Split('\n');
        var maxWidth = 0f;
        
        foreach (var line in lines) 
        {
            var lineWidth = Font.MeasureString(line);
            maxWidth = Math.Max(maxWidth, lineWidth.X);
        }

        float height = Font.LineSpacing * lines.Length;
        
        return (maxWidth, height);
    }
    
    public override void UpdateBounds()
    {
        if (Font == null) return;
        
        // empty string -> 1f size
        if (string.IsNullOrEmpty(Text)) 
        {
            UpdateBoundsBase(1f, 1f);
            return;
        }
        
        switch (Mode)
        {
            case InputMode.MultipleLines:
            {
                var (width, height) = CalculateMultipleLinesBounds();
                UpdateBoundsBase(width, height);
                break;
            }
            case InputMode.ScrollHorizontally:
            {
                // single line mode -> width is wrap length and height is 1 line
                // entered text width or wrap size
                var width = Math.Min(WrapTextSize, Font.MeasureString(GetSafeText()).X);
                var height = Font.LineSpacing;
                UpdateBoundsBase(width, height);
                break;
            }
            case InputMode.SingleLine:
            {
                base.UpdateBounds();
                break;
            }
        }
        
        if (_labelTextUi != null)
            _labelTextUi.Position = GetLabelTextPosition();
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
    
    /// <summary>
    /// Returns the position slightly left to the input and accounting for label text size
    /// </summary>
    /// <returns></returns>
    private Vector2 GetLabelTextPosition() 
    {
        if (Font == null) return Vector2.Zero;
        
        var labelSize = Font.MeasureString(_labelText);
        var left = Bounds.Left - labelSize.X - 10;
        
        return new Vector2(left, Bounds.Top);
    }
}