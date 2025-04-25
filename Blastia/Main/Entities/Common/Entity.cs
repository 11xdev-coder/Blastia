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
    
    // SPRITE
    /// <summary>
    /// If <c>A</c> is pressed, entity looks to the left. If <c>D</c>, entity looks to the right
    /// </summary>
    public float SpriteDirection { get; set; } = 1;
    protected virtual bool FlipSpriteHorizontallyOnKeyPress { get; set; }
    
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
        UpdatePosition();
        UpdateSprite();
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
        var currentWorld = PlayerManager.Instance.SelectedWorld;
        var deltaTime = (float) BlastiaGame.GameTimeElapsedSeconds;
        if (currentWorld == null || deltaTime < 0)
        {
            Position += MovementVector * deltaTime;
            IsGrounded = false;
            return;
        }
        
        // total movement player wants to move this frame
        var totalMovement = MovementVector * deltaTime;
        // how much of frame movement time is left
        var timeRemaining = 1f;
        var entityBounds = GetBounds();

        // safety
        var collisionIterations = 0;
        const int maxIterations = 5;

        // keep processing while there is still time in the frame and movement planned
        while (timeRemaining > 0f && collisionIterations < maxIterations && totalMovement.Length() > 0.0001f)
        {
            collisionIterations++;

            // assume no collision
            float minTimeOfImpact = 1f;
            Vector2 firstHitNormal = Vector2.Zero;

            // how far we want to move in this iteration
            var currentIterationDisplacement = totalMovement * timeRemaining;
            
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

        // is on ground check
        var tileIdBelow = currentWorld.GetTileIdBelow(entityBounds.Left, entityBounds.Bottom, entityBounds.Width, 1f);
        IsGrounded = tileIdBelow >= 1;

        var dragCoefficient = IsGrounded
            ? currentWorld.GetDragCoefficientTileBelow(entityBounds.Left, entityBounds.Bottom, entityBounds.Width)
            : Block.AirDragCoefficient;
        Console.WriteLine(dragCoefficient);
        ApplyGroundDrag(dragCoefficient);
        
        ApplyGravityForce();
    }

    private void UpdateSprite()
    {
        var horizontalDirection = Vector2.Zero;
        KeyboardHelper.AccumulateValueFromMap(HorizontalMovementMap, ref horizontalDirection);

        if (FlipSpriteHorizontallyOnKeyPress)
        {
            if (horizontalDirection.X > 0)
                SpriteDirection = 1;
            else if (horizontalDirection.X < 0)
                SpriteDirection = -1;
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

    private void ApplyGroundDrag(float dragCoefficient)
    {
        if (dragCoefficient > 0 && MovementVector.LengthSquared() > 0.001f)
        {
            Vector2 dragForce = -dragCoefficient * MovementVector;
            ApplyForce(dragForce);
        }
    }
    
    /// <summary>
    /// Newton's law of universal gravitation
    /// </summary>
    private void ApplyGravityForce()
    {
         if (!ApplyGravity || IsGrounded) return;
         
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
        
         ApplyForce(new Vector2(0, (float) gravityForce));
    }
}