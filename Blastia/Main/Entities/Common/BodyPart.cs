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

    public void Draw(SpriteBatch spriteBatch, Vector2 entityPosition, float scale = 1f, float direction = 1f)
    {
        Vector2 drawPosition;
        Vector2 originForDraw = Origin;
        Vector2 baseScaledOffset = RelativePosition * scale;
        float effectiveRotation = Rotation;
        var effect = SpriteEffects.None;
        
        if (direction < 0) // Flipping left
        {
            effect = SpriteEffects.FlipHorizontally;

            Vector2 mirroredOffset = new Vector2(-baseScaledOffset.X, baseScaledOffset.Y);
            drawPosition = entityPosition + mirroredOffset;

            originForDraw = new Vector2(Image.Width - Origin.X, Origin.Y);
            effectiveRotation = -Rotation;
        }
        else
        {
            drawPosition = entityPosition + baseScaledOffset;
        }
        
        spriteBatch.Draw(Image, drawPosition, null, Color, 
            effectiveRotation, originForDraw, scale, effect, 0f);
    }
}