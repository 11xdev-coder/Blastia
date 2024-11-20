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

    protected readonly Dictionary<Keys, Vector2> HorizontalMovementMap = new()
    {
        {Keys.D, new Vector2(1, 0)},
        {Keys.A, new Vector2(-1, 0)}
    };
    
    // velocity
    protected Vector2 MovementVector;
    protected float MovementSpeed;

    // GRAVITY
    protected virtual bool ApplyGravity { get; set; }
    protected const float Gravity = 1.1383124999999999999999999999998f; // G constant
    protected virtual float Mass { get; set; } = 1f; // kg
    protected bool IsGrounded { get; set; }
    
    // IMPULSE
    private Vector2 _totalImpulse;
    private Vector2 _currentImpulse;
    
    // HITBOX
    /// <summary>
    /// Entity height in blocks
    /// </summary>
    protected virtual float Height { get; set; }
    /// <summary>
    /// Entity width in blocks
    /// </summary>
    protected virtual float Width { get; set; }

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
        UpdateImpulse();
        UpdatePosition();
    }

    /// <summary>
    /// Newton's law of inertia
    /// </summary>
    private void UpdatePosition()
    {
        var newPosition = Position + MovementVector * (float) BlastiaGame.GameTimeElapsedSeconds;
        
        HandleCollision(ref newPosition);
        Position = newPosition;
    }

    /// <summary>
    /// Checks for tiles around newPosition and clamps it
    /// </summary>
    /// <param name="newPosition">New position that doesn't clip through blocks</param>
    private void HandleCollision(ref Vector2 newPosition)
    {
        var currentWorld = PlayerManager.Instance.SelectedWorld;
        if (currentWorld == null) return;

        float leftTileX = (float)Math.Floor((newPosition.X - Width * Block.Size) / Block.Size);
        float rightTileX = (float)Math.Floor((newPosition.X + Width * Block.Size) / Block.Size);
        float topTileY = (float)Math.Floor(newPosition.Y / Block.Size);
        float bottomTileY = (float)Math.Floor((newPosition.Y + Height * Block.Size) / Block.Size);

        // y is flipped, so top is subtraction, so < 0
        if (leftTileX < 0 || rightTileX >= currentWorld.WorldWidth ||
            topTileY < 0 || bottomTileY >= currentWorld.WorldHeight)
        {
            Console.WriteLine("Tf you doin here");
            return;
        }
        
        // vertical
        bool isGrounded = false;
        var startX = Math.Max(0, (int) leftTileX);
        var endX = Math.Min(currentWorld.WorldWidth - 1, (int) rightTileX);
        
        // floor
        // check only if moving down, or standing
        if (MovementVector.Y >= 0)
        {
            int floorY = (int) bottomTileY;
            for (int x = startX; x <= endX; x++)
            {
                if (currentWorld.HasTile(x, floorY))
                {
                    isGrounded = true;
                    newPosition.Y = floorY * Block.Size - Height * Block.Size; // convert to world pos
                    MovementVector.Y = 0;
                    break; // exit early
                }
            }
        }
        IsGrounded = isGrounded;
        
        // ceiling
        // check only if moving up
        if (MovementVector.Y < 0)
        {
            int ceilingY = (int) topTileY;
            for (int x = startX; x <= endX; x++)
            {
                if (currentWorld.HasTile(x, ceilingY))
                {
                    // +1 so it is the bottom of top tile 
                    newPosition.Y = (ceilingY + 1) * Block.Size;
                    MovementVector.Y = 0;
                    break; // exit early
                }
            }
        }
        
        // horizontal
        var startY = Math.Max(0, (int) topTileY);
        var endY = Math.Min(currentWorld.WorldHeight - 1, (int) bottomTileY);
        
        // left
        // check only if moving left
        if (MovementVector.X < 0)
        {
            int leftX = (int) leftTileX;
            for (int y = startY; y <= endY; y++)
            {
                if (currentWorld.HasTile(leftX, y))
                {
                    // +1 so it is right side of the block, then add Width
                    newPosition.X = (leftX + 1) * Block.Size + Width * Block.Size;
                    MovementVector.X = 0;
                    break;
                }
            }
        }
        
        // right
        // check only if moving right
        if (MovementVector.X > 0)
        {
            int rightX = (int) rightTileX;
            for (int y = startY; y <= endY; y++)
            {
                if (currentWorld.HasTile(rightX, y))
                {
                    // already left side of the block, then subtract Width
                    newPosition.X = rightX * Block.Size - Width * Block.Size;
                    MovementVector.X = 0;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Newton's second law (F = ma)
    /// </summary>
    /// <param name="force"></param>
    protected void ApplyForce(Vector2 force)
    {
        var acceleration = force / Mass;
        MovementVector += acceleration;
        Console.WriteLine($"Acceleration: {acceleration}");
    }

    /// <summary>
    /// Calculates and applies impulse to this entity
    /// </summary>
    /// <param name="force">Force applied</param>
    /// <param name="seconds">Amount of time in seconds of how long is the force applied</param>
    protected void AddImpulse(Vector2 force, float seconds)
    {
        var impulse = force * seconds;
        _totalImpulse += impulse;
    }

    /// <summary>
    /// Slowly updates impulse from 0 to total and updates the velocity
    /// </summary>
    private void UpdateImpulse()
    {
        if (_totalImpulse == Vector2.Zero) return;
    
        var deltaTime = (float)BlastiaGame.GameTimeElapsedSeconds;
        var remainingImpulse = _totalImpulse - _currentImpulse;
    
        // impulse per frame
        var impulseThisFrame = _totalImpulse / deltaTime;
    
        // finish impulse if very close to 0
        if (remainingImpulse.LengthSquared() < float.Epsilon 
            || impulseThisFrame.LengthSquared() > remainingImpulse.LengthSquared())
        {
            MovementVector += remainingImpulse;
            _totalImpulse = Vector2.Zero;
            _currentImpulse = Vector2.Zero;
        }
        else
        {
            MovementVector += impulseThisFrame;
            _currentImpulse += impulseThisFrame;
            Console.WriteLine($"Remaining impulse: {remainingImpulse.Length()}");
        }
    }
    
    /// <summary>
    /// Newton's law of universal gravitation
    /// </summary>
    private void ApplyGravityForce()
    {
        if (!ApplyGravity) return;
        
        var currentWorld = PlayerManager.Instance.SelectedWorld;
        if (currentWorld == null) return;
        
        var worldMass = World.GetMass(currentWorld.WorldWidth, currentWorld.WorldHeight);
        // m1 * m2
        var totalMass = worldMass * Mass;
        
        // find distance between Entity position and center of Hell
        // some variables
        var halfWorldWidth = currentWorld.WorldWidth * 0.5f;
        var hellWorldPosition = new Vector2(halfWorldWidth, currentWorld.WorldHeight) * Block.Size;
        // distance squared
        var r = MathUtilities.DistanceBetweenTwoPointsSquared(Position, hellWorldPosition);

        // find gravity force
        var gravityForce = Gravity * (totalMass / r);
        
        if (!IsGrounded) ApplyForce(new Vector2(0, (float) gravityForce));
        
        Console.WriteLine($"Applied gravity: {gravityForce}");
    }
}