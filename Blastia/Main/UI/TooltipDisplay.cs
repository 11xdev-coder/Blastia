using Blastia.Main.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Blastia.Main.UI;

public class TooltipLine
{
    public string Text { get; set; }
    public Color Color { get; set; }
    public float Scale { get; set; }

    public TooltipLine(string text, Color color, float scale = 1f)
    {
        Text = text;
        Color = color;
        Scale = scale;
    }
}

public class TooltipData
{
    public List<TooltipLine> Lines { get; set; } = [];
    public int Padding { get; set; } = 8;
    public Color BackgroundColor { get; set; } = new(37, 76, 167);
    public Color BorderColor { get; set; } = new(17, 47, 118);
    public int BorderThickness { get; set; } = 2;

    public void AddLine(string text, Color color, float scale = 1f)
    {
        Lines.Add(new TooltipLine(text, color, scale));
    }

    public void Clear()
    {
        Lines.Clear();
    }
}

public class TooltipDisplay
{
    private SpriteFont _font;
    private Text _hoverText;
    private bool _updatedHoverTextThisFrame;

    private Vector2 _tooltipSize;
    private Vector2 _tooltipPosition;
    private TooltipData _currentTooltipData;
    private Rectangle _tooltipBorderRect;
    private Rectangle _tooltipBackgroundRect;
    private bool _updatedTooltipThisFrame;
    
    public TooltipDisplay(SpriteFont font)
    {
        _font = font;
        
        _hoverText = new Text(Vector2.Zero, "", font)
        {
            Scale = new Vector2(0.8f, 0.8f)
        };
        _currentTooltipData = new TooltipData();
    }

    /// <summary>
    /// Updates hover text under the cursor. If at some frame no text was set, it clears.
    /// So this method should be called in <c>Update</c> methods
    /// </summary>
    /// <param name="text"></param>
    public void SetHoverText(string text)
    {
        _hoverText.Text = text;
        _updatedHoverTextThisFrame = true;
    }

    /// <summary>
    /// Called at the beginning of each frame
    /// </summary>
    public void BeginFrame()
    {
        _updatedHoverTextThisFrame = false;
        _updatedTooltipThisFrame = false;
    }

    /// <summary>
    /// Updates item tooltip.If at some frame no tooltip was set, it clears.
    /// So this method should be called in <c>Update</c> methods
    /// </summary>
    /// <param name="itemName"></param>
    /// <param name="itemType"></param>
    /// <param name="description"></param>
    public void SetTooltip(string itemName, ItemType itemType, string description = "")
    {
        _currentTooltipData.Clear();
        
        _currentTooltipData.AddLine(itemName, Color.Wheat);

        if (!string.IsNullOrEmpty(description))
        {
            _currentTooltipData.AddLine(description, Color.Wheat);
        }
        _currentTooltipData.AddLine(itemType.ToString(), Color.Wheat);
        
        _updatedTooltipThisFrame = true;
    }
    
    public void Update()
    {
        // if hover text was not set this frame, clear it
        if (!_updatedHoverTextThisFrame)
        {
            _hoverText.Text = "";
        }

        if (!_updatedTooltipThisFrame)
        {
            _currentTooltipData.Clear();
        }
        
        var desiredPosition = BlastiaGame.CursorPosition + new Vector2(20f, 20f);
        if (!string.IsNullOrEmpty(_hoverText.Text))
        {
            _hoverText.Position = desiredPosition;
            _hoverText.Update();
            _hoverText.Position = ScreenEdgeCheck(desiredPosition, _hoverText.Bounds.Right, _hoverText.Bounds.Bottom, _hoverText.Bounds.Width, _hoverText.Bounds.Height);
            _hoverText.Update(); 
        }

        // update item tooltip
        if (_currentTooltipData.Lines.Count > 0)
        {
            _tooltipSize = GetTooltipSize();
            _tooltipPosition = ScreenEdgeCheck(desiredPosition, desiredPosition.X + _tooltipSize.X, desiredPosition.Y + _tooltipSize.Y, _tooltipSize.X, _tooltipSize.Y);
            
            _tooltipBackgroundRect = new Rectangle((int) _tooltipPosition.X, (int) _tooltipPosition.Y, (int) _tooltipSize.X, (int) _tooltipSize.Y);
            _tooltipBorderRect = new Rectangle(
                (int) _tooltipPosition.X - _currentTooltipData.BorderThickness, 
                (int) _tooltipPosition.Y - _currentTooltipData.BorderThickness, 
                (int) _tooltipSize.X + _currentTooltipData.BorderThickness * 2, 
                (int) _tooltipSize.Y + _currentTooltipData.BorderThickness * 2);
        }
    }

    private Vector2 GetTooltipSize()
    {
        var maxWidth = 0f;
        var totalHeight = 0f;
        var lineSpacing = 4f;

        for (int i = 0; i < _currentTooltipData.Lines.Count; i++)
        {
            var line = _currentTooltipData.Lines[i];
            var textSize = _font.MeasureString(line.Text);
            textSize *= line.Scale;
            
            maxWidth = Math.Max(maxWidth, textSize.X);
            totalHeight += textSize.Y;

            // add line spacing except last line
            if (i < _currentTooltipData.Lines.Count - 1)
            {
                totalHeight += lineSpacing;
            }
        }
        
        return new Vector2(maxWidth + _currentTooltipData.Padding * 2, totalHeight + _currentTooltipData.Padding * 2);
    }

    private Vector2 ScreenEdgeCheck(Vector2 desiredPosition, float rightBound, float bottomBound, float width, float height)
    {
        var screenWidth = VideoManager.Instance.TargetResolution.X;
        var screenHeight = VideoManager.Instance.TargetResolution.Y;
        var adjustedPosition = desiredPosition;

        // x
        if (rightBound > screenWidth)
        {
            adjustedPosition.X = screenWidth - width;
        }
        if (adjustedPosition.X < 0)
        {
            adjustedPosition.X = 0;
        }

        if (bottomBound > screenHeight)
        {
            adjustedPosition.Y = screenHeight - height;
        }
        if (adjustedPosition.Y < 0)
        {
            adjustedPosition.Y = 0;
        }
        
        return adjustedPosition;
    }
    
    public void Draw(SpriteBatch spriteBatch)
    {
        if (!string.IsNullOrEmpty(_hoverText.Text))
        {
            _hoverText.Draw(spriteBatch);
        }

        // dont draw tooltip
        if (_currentTooltipData.Lines.Count == 0) return;

        if (_currentTooltipData.BorderThickness > 0)
        {
            spriteBatch.Draw(BlastiaGame.WhitePixel, _tooltipBorderRect, _currentTooltipData.BorderColor);
        }
        spriteBatch.Draw(BlastiaGame.WhitePixel, _tooltipBackgroundRect, _currentTooltipData.BackgroundColor);

        var currentLinePosition = _tooltipPosition + new Vector2(_currentTooltipData.Padding);
        var lineSpacing = 4f;

        foreach (var line in _currentTooltipData.Lines)
        {
            var textSize = _font.MeasureString(line.Text) * line.Scale;
            
            spriteBatch.DrawString(_font, line.Text, currentLinePosition, line.Color, 0f, Vector2.Zero, line.Scale, SpriteEffects.None, 0f);
            
            currentLinePosition.Y += textSize.Y + lineSpacing;
        }
    }
}