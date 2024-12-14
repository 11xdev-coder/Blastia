using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.Entities.Common;

public class BodyPart
{
    public Texture2D Image { get; set; }
    /// <summary>
    /// Position relative to the Entity
    /// </summary>
    public Vector2 RelativePosition { get; set; }
    public float Rotation { get; set; }
    public Vector2 Origin { get; set; }
    public Color Color { get; set; }

    public BodyPart(Texture2D image, Vector2 relativePosition, float rotation = 0f, 
        Vector2? origin = null, Color? color = null)
    {
        Image = image ?? throw new ArgumentNullException(nameof(image), "BodyPart: image cannot be null.");
        RelativePosition = relativePosition;
        Rotation = rotation;

        // if no origin -> halved Image
        Origin = origin ?? new Vector2(Image.Width * 0.5f, Image.Height * 0.5f);
        // white color by default
        Color = color ?? Color.White;
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 entityPosition, float scale = 1f)
    {
        Vector2 scaledOffset = RelativePosition * scale;
        
        Vector2 absolutePosition = entityPosition + scaledOffset;
        spriteBatch.Draw(Image, absolutePosition, null, Color, 
            Rotation, Origin, scale, SpriteEffects.None, 0f);
    }
}