using System.Drawing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.UI;

public class TooltipDisplay
{
    private SpriteFont _font;
    private Text _hoverText;
    private Func<Microsoft.Xna.Framework.Graphics.Viewport> _viewportFactory;

    public TooltipDisplay(SpriteFont font, Func<Microsoft.Xna.Framework.Graphics.Viewport> viewportFactory)
    {
        _font = font;
        _viewportFactory = viewportFactory;
        
        _hoverText = new Text(Vector2.Zero, "222", font)
        {
            Scale = new Vector2(0.8f, 0.8f)
        };
       
    }

    public void SetHoverText(string text)
    {
        _hoverText.Text = text;
    }
    
    public void Update()
    {
        if (string.IsNullOrEmpty(_hoverText.Text))
        {
            return;
        }

        var offsetX = 20f; 
        var offsetY = 20f;
        _hoverText.Position = BlastiaGame.CursorPosition + new Vector2(offsetX, offsetY);
        _hoverText.Update();
        var designScreenWidth = VideoManager.Instance.TargetResolution.X;
        var designScreenHeight = VideoManager.Instance.TargetResolution.Y;
        
        var adjustedPosition = _hoverText.Position;

        // x
        if (_hoverText.Bounds.Right > designScreenWidth) 
        {
            adjustedPosition.X = designScreenWidth - _hoverText.Bounds.Width;
        }
        if (adjustedPosition.X < 0) 
        {
            adjustedPosition.X = 0;
        }

        // y
        if (_hoverText.Bounds.Bottom > designScreenHeight)
        {
            adjustedPosition.Y = designScreenHeight - _hoverText.Bounds.Height;
        }
        if (adjustedPosition.Y < 0) 
        {
            adjustedPosition.Y = 0;
        }

        _hoverText.Position = adjustedPosition;
        _hoverText.Update(); 
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!string.IsNullOrEmpty(_hoverText.Text))
        {
            _hoverText.Draw(spriteBatch);
        }
    }
}