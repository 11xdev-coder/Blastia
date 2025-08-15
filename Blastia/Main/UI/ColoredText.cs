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
    
    public ColoredText(Vector2 position, string text, SpriteFont font) : base(position, text, font) 
    {
        ParseColoredText(text);
    }
    
    private void ParseColoredText(string text) 
    {
        _segments.Clear();

        if (string.IsNullOrEmpty(text))
            return;

        var currentColor = DrawColor;
        var currentText = "";
        
        for (int i = 0; i < text.Length; i++) 
        {
            // i + 1 wont be out of bounds
            if (i + 1 < text.Length && text[i] == _colorCodeSymbol) 
            {
                char colorCode = char.ToLower(text[i + 1]);
                
                if (_colorCodes.ContainsKey(colorCode)) 
                {
                    // add this segment if it had text
                    if (!string.IsNullOrEmpty(currentText)) 
                    {
                        _segments.Add(new TextSegment
                        {
                            Text = currentText,
                            Color = currentColor
                        });
                        currentText = "";
                    }
                    
                    // update color for new segment
                    currentColor = _colorCodes[colorCode];
                    i++; // skip color code character
                    continue;
                }
            }

            currentText += text[i];
        }
        
        // add last segment
        if (!string.IsNullOrEmpty(currentText)) 
        {
            _segments.Add(new TextSegment
            {
                Text = currentText,
                Color = currentColor
            });
        }
    }

    public override void UpdateBounds()
    {
        if (Font == null || _segments.Count == 0) return;
        
        float totalWidth = 0;
        float maxHeight = 0;
        
        foreach (var segment in _segments) 
        {
            var segmentSize = Font.MeasureString(segment.Text);
            totalWidth += segmentSize.X;
            maxHeight = Math.Max(maxHeight, segmentSize.Y);
        }

        UpdateBoundsBase(totalWidth, maxHeight);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (Font == null || _segments.Count == 0) return;

        Vector2 currentPosition = new Vector2(Bounds.X, Bounds.Y);

        foreach (var segment in _segments)
        {
            var segmentSize = Font.MeasureString(segment.Text);
            var segmentColor = segment.Color * Alpha;

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
            currentPosition.X += segmentSize.X * Scale.X;
        }
    }
}