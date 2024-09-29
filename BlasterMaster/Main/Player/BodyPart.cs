using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.Player;

public class BodyPart
{
    public Texture2D Image { get; set; }
    public Vector2 Position { get; set; }

    public BodyPart(Texture2D image, Vector2 position)
    {
        Image = image;
        Position = position;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(Image, Position, Color.White);
    }
}