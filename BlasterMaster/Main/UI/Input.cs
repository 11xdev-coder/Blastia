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
    
    public double BlinkTimer { get; private set; }
    
    public Input(Vector2 position, SpriteFont font, bool cursorVisible = false,
        double blinkInterval = 0.5f) : base(position, "", font)
    {
        _cursorVisible = cursorVisible;
        _blinkInterval = blinkInterval;
    }

    public override void Update()
    {
        base.Update();
        
        // TODO: Blink + delete + hold
        KeyboardHelper.ProcessInput(BlasterMasterGame.KeyboardState, BlasterMasterGame.PreviousKeyboardState,
            _stringBuilder);
        
        Text = _stringBuilder.ToString();
        
        Console.WriteLine(BlasterMasterGame.GameTimeElapsedSeconds);
    }

    public override void OnMenuInactive()
    {
        _stringBuilder.Clear();
        Update(); // no text in next draw
    }
}