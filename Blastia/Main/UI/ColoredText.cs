using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI;

public struct TextSegment 
{
    public string Text;
    public Color Color;
}

public class ColoredText : UIElement
{
    private List<TextSegment> _segments = [];
    private static readonly Dictionary<char, Color> _colorCodes = new()
    {
        {'0', Color.Black},
        {'1', Color.DarkBlue},
        {'2', Color.DarkGreen},
        {'3', Color.DarkCyan},
        {'4', Color.DarkRed},
        {'5', Color.DarkMagenta},
        {'6', Color.Orange},
        {'7', Color.LightGray},
        {'8', Color.DarkGray},
        {'9', Color.Blue},
        {'a', Color.Green},
        {'b', Color.Cyan},
        {'c', Color.Red},
        {'d', Color.Magenta},
        {'e', Color.Yellow},
        {'r', Color.White}
    };
    private static readonly char _colorCodeSymbol = '&';
    
    public new string? Text 
    {
        get => base.Text;
        set 
        {
            base.Text = value;
            if (value != null)
                ParseColoredText(value);
        }
    }
    private float _wrapThreshold;
    
    public ColoredText(Vector2 position, string text, SpriteFont font, float wrapThreshold = default) : base(position, text, font) 
    {
        _wrapThreshold = wrapThreshold == default ? VideoManager.Instance.TargetResolution.X : wrapThreshold;
        ParseColoredText(text);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="text"></param>
    /// <param name="color"></param>
    /// <param name="clearText">If not null, action that will be executed after adding this segment</param>
    private void AddTextSegment(string text, Color color, Action? clearText = null) 
    {
        if (string.IsNullOrEmpty(text)) return;

        _segments.Add(new TextSegment
        {
            Text = text,
            Color = color
        });
        clearText?.Invoke();
    }
    
    private void ParseColoredText(string text) 
    {
        if (Font == null) return;
        _segments.Clear();

        if (string.IsNullOrEmpty(text))
            return;

        var currentColor = DrawColor;
        var currentText = "";
        var currentWidth = 0f;
        
        for (int i = 0; i < text.Length; i++) 
        {
            // i + 1 wont be out of bounds
            if (i + 1 < text.Length && text[i] == _colorCodeSymbol) 
            {                    
                char colorCode = char.ToLower(text[i + 1]);
                
                if (_colorCodes.ContainsKey(colorCode)) 
                {
                    // add this segment if it had text
                    AddTextSegment(currentText, currentColor, () => currentText = "");
                    
                    // update color for new segment
                    currentColor = _colorCodes[colorCode];
                    i++; // skip color code character
                    continue;
                }
            }
            
            // check this symbol size            
            var symbolSize = Font.MeasureString(text[i].ToString());
            // if adding this char will exceed threshold
            if (currentWidth + symbolSize.X >= _wrapThreshold - 5) 
            {
                // add segment before wrapping
                AddTextSegment(currentText, currentColor, () => currentText = "");
                // add newline segment
                AddTextSegment("\n", currentColor);
                currentWidth = 0f;
            }
            
            currentText += text[i];
            currentWidth += symbolSize.X;
        }

        // add last segment
        AddTextSegment(currentText, currentColor);
    }

    public override void UpdateBounds()
    {
        if (Font == null || _segments.Count == 0) return;

        float maxWidth = 0f;
        float currentWidth = 0f;
        float currentHeight = Font.LineSpacing * Scale.Y; // at least one line
        
        foreach (var segment in _segments) 
        {
            if (segment.Text == "\n") 
            {
                maxWidth = Math.Max(currentWidth, maxWidth);
                currentWidth = 0f;
                currentHeight += Font.LineSpacing * Scale.Y;
            }
            else 
            {
                // add segment width
                currentWidth += Font.MeasureString(segment.Text).X * Scale.X;
            }
        }

        // last line
        maxWidth = Math.Max(currentWidth, maxWidth);
        UpdateBoundsBase(maxWidth, currentHeight);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (Font == null || _segments.Count == 0) return;

        var currentPosition = new Vector2(Bounds.X, Bounds.Y);
        var lineStartPos = currentPosition;
        var lineSpacing = Font.LineSpacing * Scale.Y;

        foreach (var segment in _segments)
        {
            var segmentColor = segment.Color * Alpha;
            
            // handle new line segments
            if (segment.Text == "\n") 
            {
                currentPosition.Y += lineSpacing;
                currentPosition.X = lineStartPos.X;
                continue;
            }

            // draw border
            if (BorderColor.A > 0)
            {
                foreach (var offset in _borderOffsets)
                {
                    spriteBatch.DrawString(Font, segment.Text, currentPosition + offset * BorderOffsetFactor, BorderColor * Alpha,
                        Rotation, Vector2.Zero, Scale, SpriteEffects.None, 0f);
                }
            }

            // draw text segment
            spriteBatch.DrawString(Font, segment.Text, currentPosition, segmentColor, Rotation, Vector2.Zero, Scale, SpriteEffects.None, 0f);

            // move positon for next segment
            var segmentSize = Font.MeasureString(segment.Text);
            currentPosition.X += segmentSize.X * Scale.X;
        }
    }
}