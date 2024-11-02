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

    public virtual ushort ID { get; set; }

    protected Entity()
    {
        
    }

    protected void SetId(ushort id)
    {
        ID = id;
    }

    protected ushort GetId() => ID;
}