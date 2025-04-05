using Blastia.Main.Blocks.Common;
using Blastia.Main.GameState;
using Blastia.Main.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    protected virtual int Height { get; set; }
    /// <summary>
    /// Entity width in blocks
    /// </summary>
    protected virtual int Width { get; set; }

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
        // ApplyGravityForce();
        // UpdateImpulse();
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
        
        Vector2 topLeft = new Vector2(left, top);
        Vector2 bottomLeft = new Vector2(left, bottom);
        Vector2 topRight = new Vector2(right, top);
        Vector2 bottomRight = new Vector2(right, bottom);
        
        BlastiaGame.RequestDebugPointDraw(topLeft, 2);
        BlastiaGame.RequestDebugPointDraw(bottomLeft, 2);
        BlastiaGame.RequestDebugPointDraw(topRight, 2);
        BlastiaGame.RequestDebugPointDraw(bottomRight, 2);
        
        // horizontal
        if (MovementVector.X > 0) // moving right
        {
            // tile to the right edge
            int tileX = (int)Math.Floor(right / Block.Size);
            // Y in tiles
            int tileYStart = (int)Math.Floor(top / Block.Size);
            int tileYEnd = (int)Math.Floor((bottom - 1) / Block.Size);

            for (int ty = tileYStart; ty <= tileYEnd; ty++)
            {
                // tile top-left
                int tileWorldX = tileX * Block.Size;
                int tileWorldY = ty * Block.Size;
                if (currentWorld.HasTile(tileWorldX, tileWorldY))
                {
                    newPosition.X = tileWorldX - (widthPixels * 0.5f);
                    MovementVector.X = 0;
                    break;
                }
            }
        }
        else if (MovementVector.X < 0) // moving left
        {
            int tileX = (int)Math.Floor(left / Block.Size);
            int tileYStart = (int)Math.Floor(top / Block.Size);
            int tileYEnd = (int)Math.Floor((bottom - 1) / Block.Size);

            for (int ty = tileYStart; ty <= tileYEnd; ty++)
            {
                int tileWorldX = tileX * Block.Size;
                int tileWorldY = ty * Block.Size;
                if (currentWorld.HasTile(tileWorldX, tileWorldY))
                {
                    newPosition.X = tileWorldX + Block.Size + (widthPixels * 0.5f);
                    MovementVector.X = 0;
                    break;
                }
            }
        }
        
        // down
        if (MovementVector.Y > 0)
        {
            int tileY = (int)Math.Floor(bottom / Block.Size);
            int tileXStart = (int)Math.Floor(left / Block.Size);
            int tileXEnd = (int)Math.Floor((right - 1) / Block.Size);

            for (int tx = tileXStart; tx <= tileXEnd; tx++)
            {
                int tileWorldX = tx * Block.Size;
                int tileWorldY = tileY * Block.Size;
                if (currentWorld.HasTile(tileWorldX, tileWorldY))
                {
                    newPosition.Y = tileWorldY - heightPixels * 0.5f;
                    MovementVector.Y = 0;
                    break;
                }
            }
        }
        else if (MovementVector.Y < 0) // up
        {
            int tileY = (int)Math.Floor(top / Block.Size);
            int tileXStart = (int)Math.Floor(left / Block.Size);
            int tileXEnd = (int)Math.Floor((right - 1) / Block.Size);

            for (int tx = tileXStart; tx <= tileXEnd; tx++)
            {
                int tileWorldX = tx * Block.Size;
                int tileWorldY = tileY * Block.Size;
                if (currentWorld.HasTile(tileWorldX, tileWorldY))
                {
                    newPosition.Y = tileWorldY + Block.Size - heightPixels * 0.5f;
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