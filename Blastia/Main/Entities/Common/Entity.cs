using System.Globalization;
using Blastia.Main.Blocks.Common;
using Blastia.Main.GameState;
using Blastia.Main.Physics;
using Blastia.Main.Sounds;
using Blastia.Main.Utilities;
using MathNet.Numerics.LinearAlgebra;
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
    
    // SPRITE
    /// <summary>
    /// If <c>A</c> is pressed, entity looks to the left. If <c>D</c>, entity looks to the right
    /// </summary>
    public float SpriteDirection { get; set; } = 1;
    protected virtual bool FlipSpriteHorizontallyOnKeyPress { get; set; }
    
    // velocity
    protected Vector2 MovementVector;
    protected float MovementSpeed;
    protected float TimeToMaxSpeed;
    protected const float FixedTimeStep = 1 / 240f; // simulate velocity at 240 FPS
    
    /// <summary>
    /// How much friction entity has with surfaces. <c>1.0</c> is normal friction, <c>&lt; 1.0</c> is more slippery
    /// </summary>
    protected virtual float FrictionMultiplier { get; set; } = 1f;
    /// <summary>
    /// Bounciness multiplier. <c>0.0</c> is no bounce, <c>1.0</c> is a perfect bounce.
    /// </summary>
    protected virtual float Bounciness { get; set; } = 0f;

    // GRAVITY
    protected virtual bool ApplyGravity { get; set; }
    protected const float Gravity = 68.521488f; // G constant
    protected virtual float Mass { get; set; } = 1f; // kg
    /// <summary>
    /// <c>True</c> if player touches the ground (1 pixel above ground)
    /// </summary>
    protected bool IsGrounded { get; set; }
    /// <summary>
    /// <c>True</c> if player is a little above ground (5 pixels above ground)
    /// </summary>
    protected bool CanJump { get; set; }
    
    // IMPULSE
    private Vector2 _totalImpulse;
    private Vector2 _currentImpulse;
    
    // HITBOX
    /// <summary>
    /// Entity height in blocks
    /// </summary>
    public virtual float Height { get; set; }
    /// <summary>
    /// Entity width in blocks
    /// </summary>
    public virtual float Width { get; set; }
    
    /// <summary>
    /// Entity max health
    /// </summary>
    public virtual float MaxLife { get; set; }
    /// <summary>
    /// Entity current health
    /// </summary>
    public float Life
    {
        get => _life;
        set => Properties.OnValueChangedProperty(ref _life, value, OnLifeChanged);
    }
    private float _life;
    /// <summary>
    /// Immunity time after being hit in seconds
    /// </summary>
    public virtual float ImmunitySeconds { get; set; } = 0.7f;
    protected float ImmunityTimer;
    /// <summary>
    /// Duration when entity will flicker after being hit
    /// </summary>
    public virtual float VisualFlickerImmunitySeconds { get; set; } = 0.35f;
    protected float VisualFlickerTimer;
    
    public virtual SoundID HitSound { get; set; }
    
    protected virtual ushort ID { get; set; }
    
    protected Entity(Vector2 position, float initialScaleFactor)
    {
        Position = position;
        Scale = initialScaleFactor;
        
        InitializeLife();
    }

    private void InitializeLife()
    {
        Life = MaxLife;
    }

    #region Life
    /// <summary>
    /// Called whenever <c>Life</c> changed
    /// </summary>
    protected virtual void OnLifeChanged()
    {
        
    }

    /// <summary>
    /// Tries to damage the entity accounting to its <c>ImmunitySeconds</c>
    /// </summary>
    /// <param name="damage"></param>
    public void TryDamage(float damage)
    {
        if (ImmunityTimer <= 0)
        {
            // apply damage
            ApplyDamage(damage);
        }
    }

    /// <summary>
    /// Applies damage ignoring <c>ImmunitySeconds</c>
    /// </summary>
    /// <param name="damage"></param>
    public void ApplyDamage(float damage)
    {
        Life -= damage;
        ImmunityTimer = ImmunitySeconds;
        VisualFlickerTimer = VisualFlickerImmunitySeconds;
        BlastiaGame.TooltipDisplay?.AddBouncingText(damage.ToString(CultureInfo.CurrentCulture), Color.DarkRed, Position, new Vector2(1.6f, 1.6f));
        
        if (Life <= 0)
        {
            OnKill();
            return;
        }

        OnHit();
        SoundEngine.PlaySound(HitSound);
    }

    /// <summary>
    /// Called whenever entity is killed (Life &lt;= 0). By default removes this entity
    /// </summary>
    protected virtual void OnKill()
    {
        BlastiaGame.RequestRemoveEntity(this);
    }

    protected virtual void OnHit()
    {
        
    }
    
    #endregion

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
        if (ImmunityTimer > 0)
        {
            ImmunityTimer -= (float) BlastiaGame.GameTimeElapsedSeconds;
        }
        if (VisualFlickerTimer > 0)
        {
            VisualFlickerTimer -= (float) BlastiaGame.GameTimeElapsedSeconds;
        }
        
        UpdatePosition();
        UpdateSprite();
    }

    public Rectangle GetBounds()
    {
        var widthPixels = Width * Block.Size;
        var heightPixels = Height * Block.Size;
        return new Rectangle(
            (int) (Position.X - widthPixels * 0.5f), 
            (int) (Position.Y - heightPixels * 0.5f), 
            (int) widthPixels, (int) heightPixels);
    }
    
    private void UpdatePosition()
    {
        var currentWorld = PlayerNWorldManager.Instance.SelectedWorld;
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
            var potentialColliders = Collision.GetPotentialCollidersInRectangle(sweptBounds);
            
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

                // horizontal collision
                if (Math.Abs(firstHitNormal.X) > Math.Abs(firstHitNormal.Y)) 
                {
                    MovementVector.X = 0f;
    
                    // avoid hitting the wall when moving up
                    if (MovementVector.Y < 0)
                    {
                        // move slightly away from the wall
                        Position += new Vector2(firstHitNormal.X * 0.1f, 0);
                    }
                }
                else // vertical
                {
                    if (firstHitNormal.Y < -0.1f) // hit ground (normal points up)
                    {
                        // bounciness
                        MovementVector.Y *= -Bounciness;
                        // kill bounce if too small
                        if (Math.Abs(MovementVector.Y) < 1f) 
                        {
                            MovementVector.Y = 0f;
                        }
                    }
                    else if (firstHitNormal.Y > 0.1f) // hit ceiling (normal points down)
                    {
                        MovementVector.Y = 0f;
                    }
                }

                totalMovement = MovementVector * deltaTime;
            }
        }

        // is on ground check
        var strictTileBelow = currentWorld.GetBlockInstanceBelow(entityBounds.Left, entityBounds.Bottom, entityBounds.Width, 1f, TileLayer.Ground);
        IsGrounded = strictTileBelow != null && strictTileBelow.Block.IsCollidable;
        
        var tileBelow = currentWorld.GetBlockInstanceBelow(entityBounds.Left, entityBounds.Bottom, entityBounds.Width, 3f, TileLayer.Ground);
        CanJump = tileBelow != null && tileBelow.Block.IsCollidable;
        
        var dragCoefficient = IsGrounded
            ? currentWorld.GetDragCoefficientTileBelow(entityBounds.Left, entityBounds.Bottom, entityBounds.Width, TileLayer.Ground)
            : Block.AirDragCoefficient;
        ApplyGroundDrag(dragCoefficient * FrictionMultiplier);
        
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
        var deltaTime = (float) BlastiaGame.GameTimeElapsedSeconds;
        var acceleration = force / Mass;
        MovementVector += acceleration * deltaTime;
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
            // stop movement when speed is too low
            if (IsGrounded && Math.Abs(MovementVector.X) < 0.1f)
            {
                MovementVector.X = 0f;
            }
            
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
         
         var currentWorld = PlayerNWorldManager.Instance.SelectedWorld;
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
         var minDistance = 50f * 50f;
         r = Math.Max(r, minDistance);
        
         // find gravity force
         var gravityForce = Gravity * (totalMass / r);
        
         ApplyForce(new Vector2(0, (float) gravityForce));
    }
}