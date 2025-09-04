using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI;

public struct TextSegment 
{
    public string Text;
    public Color Color;
}

public struct GifSegment 
{
    public AnimatedGif Gif;
}

public class ColoredText : UIElement
{
    private List<TextSegment> _segments = [];
    private List<GifSegment> _gifSegments = [];
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
    /// <param name="clearText">If specified, action that will be executed after adding this segment</param>
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
            // check for gif
            // gif: can fit
            if (text[i] == '[' && i + 5 < text.Length && text.Substring(i, 5) == "[gif:") 
            {
                var closingBracketIndex = text.IndexOf(']', i);
                if (closingBracketIndex != -1) 
                {
                    var gifTag = text.Substring(i, closingBracketIndex - i + 1);
                    if (gifTag.StartsWith("[gif:") && gifTag.EndsWith("]")) 
                    {
                        // add text segment before GIF
                        AddTextSegment(currentText, currentColor, () => currentText = "");

                        var gifUrl = gifTag.Substring(5, gifTag.Length - 6); // remove [gif: and ]
                        var animatedGif = new AnimatedGif(Vector2.Zero, gifUrl);

                        _gifSegments.Add(new GifSegment()
                        {
                            Gif = animatedGif
                        });

                        // add placeholder text
                        AddTextSegment(" ", currentColor);

                        // skip to the end
                        i = closingBracketIndex;
                        continue;
                    }
                }
            }
            
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
            // at least one more same char should fit
            if (currentWidth + symbolSize.X >= _wrapThreshold - symbolSize.X - 10) 
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

    public override void Update()
    {
        base.Update();
        
        foreach (var gif in _gifSegments) 
        {
            gif.Gif.Update();
            gif.Gif.Alpha = Alpha; // copy alpha
        }
    }

    public override void UpdateBounds()
    {
        if (Font == null || _segments.Count == 0) return;

        float maxWidth = 0f;
        float currentWidth = 0f;
        float currentHeight = Font.LineSpacing * Scale.Y; // at least one line
        var gifIndex = 0;
        
        // TODO: dont download the gif, immediatly push the message up, multi-line support
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
            
            // if this is a gif
            if (segment.Text == " "  && gifIndex < _gifSegments.Count)  
            {
                // add its height
                var gif = _gifSegments[gifIndex].Gif;
                if (gif.Texture != null) currentHeight += gif.Texture.Height;
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
        var gifIndex = 0;

        foreach (var segment in _segments)
        {            
            // handle new line segments
            if (segment.Text == "\n") 
            {
                currentPosition.Y += lineSpacing;
                currentPosition.X = lineStartPos.X;
                continue;
            }
            
            // check if this is a gif placeholder
            if (segment.Text == " "  && gifIndex < _gifSegments.Count) 
            {
                var gifSegment = _gifSegments[gifIndex];
                gifSegment.Gif.Position = currentPosition;
                gifSegment.Gif.Draw(spriteBatch);

                // move text
                if (gifSegment.Gif.Texture != null) 
                {
                    currentPosition.X += gifSegment.Gif.Texture.Width * gifSegment.Gif.Scale.X;
                }

                gifIndex += 1;
                continue;
            }

            base.Draw(spriteBatch, currentPosition, segment.Text, default, segment.Color);

            // move positon for next segment
            var segmentSize = Font.MeasureString(segment.Text);
            currentPosition.X += segmentSize.X * Scale.X;
        }
    }
}