using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.Player;

public class BodyPart
{
    public Texture2D Image { get; set; }
    public Vector2 Position { get; set; }
    public float Rotation { get; set; }
    public Vector2 Origin { get; set; }

    public BodyPart(Texture2D image, Vector2 position, float rotation = 0f, 
        Vector2? origin = null)
    {
        Image = image ?? throw new ArgumentNullException(nameof(image), "BodyPart: image cannot be null.");
        Position = position;
        Rotation = rotation;

        // if no origin -> halved Image
        Origin = origin ?? new Vector2(Image.Width * 0.5f, Image.Height * 0.5f);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(Image, Position, Color.White);
    }
}