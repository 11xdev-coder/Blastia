using System.Text;
using BlasterMaster.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI;

public class Input : UIElement
{
    private StringBuilder _stringBuilder = new();

    private double _blinkInterval;
    private bool _cursorVisible;
    private bool _shouldDraw;

    private double BlinkTimer { get; set; }
    private Color CursorColor { get; set; }
    private int CursorWidth { get; set; }
    private int CursorHeight { get; set; }
    
    public Input(Vector2 position, SpriteFont font, bool cursorVisible = false,
        double blinkInterval = 0.5f, Color? cursorColor = default, 
        int cursorWidth = 2, int cursorHeight = 30) : base(position, "", font)
    {
        _cursorVisible = cursorVisible;
        _shouldDraw = cursorVisible;
        _blinkInterval = blinkInterval;

        CursorColor = cursorColor ?? Color.White;
        CursorWidth = cursorWidth;
        CursorHeight = cursorHeight;
    }

    public override void Update()
    {
        base.Update();
        
        // TODO: Blink + delete + hold
        KeyboardHelper.ProcessInput(BlasterMasterGame.KeyboardState, BlasterMasterGame.PreviousKeyboardState,
            _stringBuilder);
        
        Text = _stringBuilder.ToString();

        if (_cursorVisible) Blink();
    }

    private void Blink()
    {
        BlinkTimer += BlasterMasterGame.GameTimeElapsedSeconds;
        if (BlinkTimer >= _blinkInterval)
        {
            _shouldDraw = !_shouldDraw;
            BlinkTimer -= _blinkInterval;
        }
    }

    public override void OnMenuInactive()
    {
        _stringBuilder.Clear();
        _shouldDraw = false;
        Update(); // no text in next draw
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);

        if (_shouldDraw)
        {
            var cursorPosition = new Vector2(Bounds.Right, Bounds.Center.Y - CursorHeight * 0.5f);
            
            var cursorRectangle = new Rectangle((int)cursorPosition.X, (int)cursorPosition.Y, 
                CursorWidth, CursorHeight);
            spriteBatch.Draw(BlasterMasterGame.WhitePixel, cursorRectangle, CursorColor);
        }
    }
}