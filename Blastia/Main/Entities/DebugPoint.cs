using Blastia.Main.Entities.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.Entities;

[Entity(Id = EntityID.DebugPoint)]
public class DebugPoint : Entity
{
    private readonly BodyPart _dot;
    
    public DebugPoint(Vector2 position, float initialScaleFactor) : base(position, initialScaleFactor)
    {
        SetId(EntityID.DebugPoint);

        _dot = new BodyPart(BlastiaGame.WhitePixel, Vector2.Zero, color: Color.Red);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        Draw(spriteBatch, Position);
    }

    public override void Draw(SpriteBatch spriteBatch, Vector2 scaledPosition, float scale = 1)
    {
        _dot.Draw(spriteBatch, scaledPosition, scale * Scale);
    }
}