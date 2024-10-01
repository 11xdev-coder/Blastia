using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlasterMaster.Main.Player;

public class BodyPart
{
    public Texture2D Image { get; set; }
    /// <summary>
    /// Position relative to the Entity
    /// </summary>
    public Vector2 RelativePosition { get; set; }
    public float Rotation { get; set; }
    public Vector2 Origin { get; set; }

    public BodyPart(Texture2D image, Vector2 relativePosition, float rotation = 0f, 
        Vector2? origin = null)
    {
        Image = image ?? throw new ArgumentNullException(nameof(image), "BodyPart: image cannot be null.");
        RelativePosition = relativePosition;
        Rotation = rotation;

        // if no origin -> halved Image
        Origin = origin ?? new Vector2(Image.Width * 0.5f, Image.Height * 0.5f);
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 entityPosition)
    {
        Vector2 absolutePosition = entityPosition + RelativePosition;
        spriteBatch.Draw(Image, absolutePosition, Color.White);
    }
}