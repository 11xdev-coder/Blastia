using Blastia.Main.Entities.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.Entities;

[Entity(Id = EntityID.DebugPoint)]
public class DebugPoint : Entity
{
    public EventHandler? RemoveEvent;
    private readonly BodyPart _dot;
    
    public DebugPoint(Vector2 position, float initialScaleFactor, EventHandler? removeEvent = null) : base(position, initialScaleFactor)
    {
        SetId(EntityID.DebugPoint);

        RemoveEvent = removeEvent;
        _dot = new BodyPart(BlastiaGame.WhitePixel, new Vector2(0f, 0f));
    }

    public override void Update()
    {
        RemoveEvent?.Invoke(this, EventArgs.Empty);
        base.Update();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        Draw(spriteBatch, Position);
    }

    public override void Draw(SpriteBatch spriteBatch, Vector2 scaledPosition, float scale = 1)
    {
        _dot.Draw(spriteBatch, scaledPosition, scale);
    }
}