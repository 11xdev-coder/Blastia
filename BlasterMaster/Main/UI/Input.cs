using System.Text;
using BlasterMaster.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI;

public class Input : UIElement
{
    private StringBuilder _stringBuilder = new();
    
    public Input(Vector2 position, SpriteFont font) : base(position, "", font)
    {
    }

    public override void Update()
    {
        base.Update();
        
        KeyboardHelper.ProcessInput(BlasterMasterGame.KeyboardState, BlasterMasterGame.PreviousKeyboardState,
            _stringBuilder);
        
        Text = _stringBuilder.ToString();
    }

    public override void OnMenuInactive()
    {
        _stringBuilder.Clear();
    }
}