using BlasterMaster.Main.UI.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.UI;

public class Slider : Button
{
    public override bool Draggable => true;

    public Slider(Vector2 position, SpriteFont font) : 
        base(position, "O", font, null)
    {
    }

    public override void Update()
    {
        base.Update();
    }
}