using Blastia.Main.Blocks.Common;
using Blastia.Main.GameState;
using Blastia.Main.Utilities;
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
    
    // velocity
    protected Vector2 MovementVector;
    protected float MovementSpeed;

    // GRAVITY
    protected virtual bool ApplyGravity { get; set; }
    protected const float Gravity = 0.018971875f; // G constant
    protected virtual float Mass { get; set; } = 1f; // kg
    
    // HITBOX
    /// <summary>
    /// Entity height in blocks
    /// </summary>
    protected virtual float Height { get; set; }

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
    /// Updates position and adds all natural forces
    /// </summary>
    public override void Update()
    {
        ApplyGravityForce();
        UpdatePosition();
    }

    /// <summary>
    /// Newton's law of inertia
    /// </summary>
    private void UpdatePosition()
    {
        Position += MovementVector;
    }

    /// <summary>
    /// Newton's second law (F = ma)
    /// </summary>
    /// <param name="force"></param>
    private void ApplyForce(Vector2 force)
    {
        var acceleration = force / Mass;
        MovementVector += acceleration;
        Console.WriteLine($"Acceleration: {acceleration}");
    }
    
    /// <summary>
    /// Newton's law of universal gravitation
    /// </summary>
    private void ApplyGravityForce()
    {
        if (ApplyGravity)
        {
            var currentWorld = PlayerManager.Instance.SelectedWorld;
            var x = Position.X / Block.Size;
            var y = Position.Y / Block.Size + Height; // correct from top-left corner to bottom
            
            // less than 0 -> air
            if (currentWorld != null && x > 0 && x < currentWorld.WorldWidth && y > 0 && y < currentWorld.WorldHeight &&
                currentWorld.GetTile((int) x, (int) y) < 1)
            {
                var worldMass = World.GetMass(currentWorld.WorldWidth, currentWorld.WorldHeight);
                // m1 * m2
                var totalMass = worldMass * Mass;
                
                // find distance between Entity position and center of Hell
                // some variables
                var halfWorldWidth = currentWorld.WorldWidth * 0.5f;
                var hellWorldPosition = new Vector2(halfWorldWidth, currentWorld.WorldHeight) * 8;
                // distance squared
                var r = MathUtilities.DistanceBetweenTwoPointsSquared(Position, hellWorldPosition);

                // find gravity force
                var gravityForce = Gravity * (totalMass / r);
                ApplyForce(new Vector2(0, (float) gravityForce));
                Console.WriteLine($"Applied gravity: {gravityForce}");
            }
        }
    }
}