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

    public Action? OnAnyGifLoaded;
    
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
        _gifSegments.Clear();

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
                        animatedGif.OnGifLoaded += () => 
                        {
                            UpdateBounds();
                            OnAnyGifLoaded?.Invoke();
                            Console.WriteLine("Gif loaded");
                        };

                        _gifSegments.Add(new GifSegment()
                        {
                            Gif = animatedGif
                        });
                        
                        // gif width for wrapping
                        var gifWidth = animatedGif.Texture != null ? animatedGif.Texture.Width * animatedGif.Scale.X : 50f;
                        if (currentWidth + gifWidth >= _wrapThreshold - 10) 
                        {
                            // add new line before GIF symbol
                            AddTextSegment("\n", currentColor);
                            currentWidth = 0f;
                        }                        

                        // add placeholder text
                        AddTextSegment("\x01", currentColor);

                        currentWidth += gifWidth;

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

    private delegate void OnTextCallback(TextSegment segment, Vector2 position);
    private delegate void OnGifCallback(GifSegment gifSegment, Vector2 position);
    private delegate void OnNewLineCallback(ref Vector2 position, ref float maxLineHeight);
    
    /// <summary>
    /// Iteration logic
    /// </summary>
    private void IterateSegments(OnTextCallback? onText = null, OnGifCallback? onGif = null, OnNewLineCallback? onNewLine = null) 
    {
        if (Font == null) return;
        
        var currentPosition = new Vector2(Bounds.X, Bounds.Y);
        var lineStartPos = currentPosition;
        var lineSpacing = Font.LineSpacing * Scale.Y;
        var currentLineMaxHeight = lineSpacing;
        var gifIndex = 0;
        
        foreach (var segment in _segments) 
        {
            if (segment.Text == "\n") 
            {
                onNewLine?.Invoke(ref currentPosition, ref currentLineMaxHeight);
                currentPosition.Y += currentLineMaxHeight;
                currentPosition.X = lineStartPos.X;
                
                currentLineMaxHeight = lineSpacing; // reset max height for this line
                continue;
            }
            
            // special gif char
            if (segment.Text == "\x01" && gifIndex < 0) 
            {
                var gifSegment = _gifSegments[gifIndex];
                onGif?.Invoke(gifSegment, currentPosition);
                gifIndex++;
                
                if (gifSegment.Gif.Texture != null) 
                {
                    currentPosition.X += gifSegment.Gif.Texture.Width * gifSegment.Gif.Texture.Width;
                    // track max height
                    currentLineMaxHeight = Math.Max(currentLineMaxHeight, gifSegment.Gif.Texture.Height * gifSegment.Gif.Texture.Height);
                }
                continue;
            }

            // else -> default text
            onText?.Invoke(segment, currentPosition);

            currentPosition.X += Font.MeasureString(segment.Text).X * Scale.X;
        }
    }

    public override void UpdateBounds()
    {
        if (Font == null || _segments.Count == 0) return;

        var currentWidth = 0f;
        var maxWidth = 0f; // final width
        var totalHeight = 0f;
        
        IterateSegments(onText: (segment, position) =>
        {
            // dont track gifs
            if (segment.Text != "\x01") 
            {
                // position.X - bounds.X -> how much pixels from the start + add new symbol size
                currentWidth = position.X - Bounds.X + Font.MeasureString(segment.Text).X * Scale.X;
                maxWidth = Math.Max(currentWidth, maxWidth);
            }
        },
        onGif: (gifSegment, position) =>
        {
            if (gifSegment.Gif.Texture != null) 
            {
                currentWidth = position.X - Bounds.X + gifSegment.Gif.Texture.Width * Scale.X;
                maxWidth = Math.Max(currentWidth, maxWidth);
            }
        },
        onNewLine: (ref Vector2 position, ref float maxLineHeight) =>
        {
            totalHeight += maxLineHeight;
            currentWidth = 0f;
        });

        UpdateBoundsBase(maxWidth, totalHeight);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        IterateSegments(onText: (segment, position) =>
        {
            // dont draw gifs
            if (segment.Text != "\x01") 
            {
                base.Draw(spriteBatch, position, segment.Text, default, segment.Color);
            }
        },
        onGif: (gifSegment, position) =>
        {
            gifSegment.Gif.Position = position;
            gifSegment.Gif.Draw(spriteBatch);
        });
    }
}