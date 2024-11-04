using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Object = Blastia.Main.GameState.Object;

namespace Blastia.Main.Entities.Common;

public abstract class Entity : Object
{
    // map for movement keys and their vectors
    protected readonly Dictionary<Keys, Vector2> MovementMap = new()
    {
        {Keys.A, new Vector2(-1, 0)},
        {Keys.D, new Vector2(1, 0)},
        {Keys.W, new Vector2(0, -1)},
        {Keys.S, new Vector2(0, 1)}
    };
    
    protected Vector2 MovementVector;
    protected float MovementSpeed;

    protected virtual ushort ID { get; set; }

    protected Entity(Vector2 position, float initialScaleFactor)
    {
        Position = position;
        Scale = initialScaleFactor;
    }

    /// <summary>
    /// Call in constructor to set the ID
    /// </summary>
    /// <param name="id">EntityID</param>
    protected void SetId(ushort id)
    {
        ID = id;
    }

    protected ushort GetId() => ID;

    /// <summary>
    /// Adds MovementVector to Position. Call this when Entity should move and MovementVector has been set
    /// </summary>
    protected void UpdatePosition()
    {
        Position += MovementVector;
    }
}