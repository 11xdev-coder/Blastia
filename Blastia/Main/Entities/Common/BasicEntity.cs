using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blastia.Main.Entities.Common;

/// <summary>
/// Placeholder entity, use only to set its ID to a different entity
/// </summary>
[Entity(Id = EntityID.BasicEntity)]
public class BasicEntity : Entity
{
    public BasicEntity(Vector2 position, float initialScaleFactor) : base(position, initialScaleFactor) 
    {
        SetId(EntityID.BasicEntity);
        AssignNetworkId();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        throw new NotImplementedException();
    }

    public override void Draw(SpriteBatch spriteBatch, Vector2 scaledPosition, float scale = 1)
    {
        throw new NotImplementedException();
    }
}