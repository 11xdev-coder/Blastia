using Blastia.Main.Blocks.Common;
using Blastia.Main.GameState;
using Blastia.Main.Physics;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Vulkan;
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
    protected virtual int Height { get; set; }
    /// <summary>
    /// Entity width in blocks
    /// </summary>
    protected virtual int Width { get; set; }
    private int _distancePerCollisionStep = Block.Size;

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
    /// Updates position and adds all natural forces. Must be called AFTER all velocity changes.
    /// </summary>
    public override void Update()
    {
        // ApplyGravityForce();
        // UpdateImpulse();
        UpdatePosition();
    }

    private Rectangle GetBounds()
    {
        var widthPixels = Width * Block.Size;
        var heightPixels = Height * Block.Size;
        return new Rectangle(
            (int) (Position.X - widthPixels * 0.5f), 
            (int) (Position.Y - heightPixels * 0.5f), 
            widthPixels, heightPixels);
    }
    
    private void UpdatePosition()
    {
        GetBounds();
        var currentWorld = PlayerManager.Instance.SelectedWorld;
        var deltaTime = (float) BlastiaGame.GameTimeElapsedSeconds;
        if (currentWorld == null || deltaTime < 0)
        {
            Position += MovementVector * deltaTime;
            return;
        }
        
        // total movement player wants to move this frame
        var totalMovement = MovementVector * deltaTime;
        // how much of frame movement time is left
        var timeRemaining = 1f;

        // safety
        var collisionIterations = 0;
        const int MaxIterations = 5;

        // keep processing while there is still time in the frame and movement planned
        while (timeRemaining > 0f && collisionIterations < MaxIterations && totalMovement.Length() > 0.0001f)
        {
            collisionIterations++;

            // assume no collision
            float minTimeOfImpact = 1f;
            Vector2 firstHitNormal = Vector2.Zero;

            // how far we want to move in this iteration
            var currentIterationDisplacement = totalMovement * timeRemaining;
            var entityBounds = GetBounds();
            
            // broadphase
            // only check tiles that are possibly in the way
            var sweptBounds = Collision.GetSweptBounds(entityBounds, currentIterationDisplacement);
            var potentialColliders = Collision.GetTilesInRectangle(currentWorld, sweptBounds);
            
            // narrow phase
            foreach (var collider in potentialColliders)
            {
                if (Collision.SweptAabb(collider, entityBounds, currentIterationDisplacement,
                        out var timeOfImpact, out var normal))
                {
                    // check if collision earlier than any other collision
                    if (timeOfImpact < minTimeOfImpact)
                    {
                        // mark it as first
                        minTimeOfImpact = timeOfImpact;
                        firstHitNormal = normal;
                    }
                }
            }
            
            // move the player
            // move full distance (minTimeOfImpact == 1)
            // or move a part of it (minTimeOfImpact < 1)
            var movementThisIteration = currentIterationDisplacement * minTimeOfImpact;
            Position += movementThisIteration;
            
            // consume `minTimeOfImpact` time from the whole frame
            timeRemaining *= 1 - minTimeOfImpact;
            
            // collision respond
            if (minTimeOfImpact < 1f)
            {
                Position += firstHitNormal * 0.0001f;

                if (Math.Abs(firstHitNormal.X) > 0.001f)
                {
                    MovementVector.X = 0f;
                }

                if (Math.Abs(firstHitNormal.Y) > 0.001f)
                {
                    MovementVector.Y = 0f;
                }

                totalMovement = MovementVector * deltaTime;
            }
        }
    }

    private void HandleCollision(ref Vector2 newPosition)
    {
        var currentWorld = PlayerManager.Instance.SelectedWorld;
        if (currentWorld == null) return;

        int heightPixels = Height * Block.Size;
        int widthPixels = Width * Block.Size;

        float left = newPosition.X - widthPixels * 0.5f;
        float top = newPosition.Y - heightPixels * 0.5f;
        float right = newPosition.X + widthPixels * 0.5f;
        float bottom = newPosition.Y + heightPixels * 0.5f;
        
        int tileXStart = (int) Math.Floor(left / Block.Size);
        int tileXEnd = (int) Math.Floor((right - 1) / Block.Size);
        int tileYStart = (int) Math.Floor(top / Block.Size);
        int tileYEnd = (int) Math.Floor((bottom - 1) / Block.Size);

        // horizontal
        if (MovementVector.X != 0) 
        {
            // right and left
            int direction = MovementVector.X > 0 ? 1 : -1;
            int tileX = direction > 0 ? tileXEnd : tileXStart;
            int tileWorldX = tileX * Block.Size;

            for (int ty = tileYStart; ty <= tileYEnd; ty++)
            {
                int tileWorldY = ty * Block.Size;
                if (currentWorld.HasTile(tileWorldX, tileWorldY))
                {
                    newPosition.X = direction > 0 
                        ? tileWorldX - widthPixels * 0.5f // right
                        : tileWorldX + Block.Size + widthPixels * 0.5f; // left
                    MovementVector.X = 0;
                    break;
                }
            }
        }
        
        // vertical
        if (MovementVector.Y != 0)
        {
            int direction = MovementVector.Y > 0 ? 1 : -1; // down and up
            int tileY = direction > 0 ? tileYEnd : tileYStart;
            int tileWorldY = tileY * Block.Size;

            for (int tx = tileXStart; tx <= tileXEnd; tx++)
            {
                int tileWorldX = tx * Block.Size;
                if (currentWorld.HasTile(tileWorldX, tileWorldY))
                {
                    newPosition.Y = direction > 0
                        ? tileWorldY - heightPixels * 0.5f // bottom
                        : tileWorldY + Block.Size + heightPixels * 0.5f; // top
                    MovementVector.Y = 0;
                    break;
                }
            }
        }
        
        if (MovementVector.Y < 0) // up
        {
            var tileWorldY = tileYStart * Block.Size;
            for (var tx = tileXStart; tx <= tileXEnd; tx++)
            {
                var tileWorldX = tx * Block.Size;
                if (currentWorld.HasTile(tileWorldX, tileWorldY))
                {
                    newPosition.Y = tileWorldY + Block.Size + heightPixels * 0.5f;
                    MovementVector.Y = 0;
                    break;
                }
            }
        }
        else if (MovementVector.Y > 0) // down
        {
            var tileWorldY = tileYEnd * Block.Size;
            for (var tx = tileXStart; tx <= tileXEnd; tx++)
            {
                var tileWorldX = tx * Block.Size;
                if (currentWorld.HasTile(tileWorldX, tileWorldY))
                {
                    newPosition.Y = tileWorldY - heightPixels * 0.5f;
                    MovementVector.Y = 0;
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
        //Console.WriteLine($"Acceleration: {acceleration}");
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
        //if (!ApplyGravity) return;
        //ApplyForce(new Vector2(0, 0));
        // var currentWorld = PlayerManager.Instance.SelectedWorld;
        // if (currentWorld == null) return;
        //
        // var worldMass = World.GetMass(currentWorld.WorldWidth, currentWorld.WorldHeight);
        // // m1 * m2
        // var totalMass = worldMass * Mass;
        //
        // // find distance between Entity position and center of Hell
        // // some variables
        // var halfWorldWidth = currentWorld.WorldWidth * 0.5f;
        // var hellWorldPosition = new Vector2(halfWorldWidth, currentWorld.WorldHeight) * Block.Size;
        // // distance squared
        // var r = MathUtilities.DistanceBetweenTwoPointsSquared(Position, hellWorldPosition);
        //
        // // find gravity force
        // var gravityForce = Gravity * (totalMass / r);
        //
        // if (!IsGrounded) ApplyForce(new Vector2(0, (float) gravityForce));
        //
        // Console.WriteLine($"Applied gravity: {gravityForce}");
    }
}