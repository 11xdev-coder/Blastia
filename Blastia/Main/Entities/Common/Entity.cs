using Blastia.Main.Blocks.Common;
using Blastia.Main.GameState;
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
    /// Updates position and adds all natural forces
    /// </summary>
    public override void Update()
    {
        // ApplyGravityForce();
        // UpdateImpulse();
        UpdatePosition();
    }
    
    Rectangle GetPlayerRect(Vector2 pos)
    {
        int widthPixels = Width * Block.Size;
        int heightPixels = Height * Block.Size;
        return new Rectangle(
            (int)(pos.X - widthPixels * 0.5f),
            (int)(pos.Y - heightPixels * 0.5f),
            widthPixels,
            heightPixels
        );
    }

    private (float toi, Vector2 normal) SweptAABB(Rectangle movingRect, Vector2 velocity, Rectangle staticRect)
    {
        // Expand static rect by 1px to prevent tunneling
        staticRect = new Rectangle(
            staticRect.X - 1,
            staticRect.Y - 1,
            staticRect.Width + 2,
            staticRect.Height + 2
        );

        Vector2 entryDist = new Vector2();
        Vector2 exitDist = new Vector2();

        // X-axis calculations
        if (velocity.X > 0)
        {
            entryDist.X = staticRect.Left - movingRect.Right;
            exitDist.X = staticRect.Right - movingRect.Left;
        }
        else
        {
            entryDist.X = staticRect.Right - movingRect.Left;
            exitDist.X = staticRect.Left - movingRect.Right;
        }

        // Y-axis calculations
        if (velocity.Y > 0)
        {
            entryDist.Y = staticRect.Top - movingRect.Bottom;
            exitDist.Y = staticRect.Bottom - movingRect.Top;
        }
        else
        {
            entryDist.Y = staticRect.Bottom - movingRect.Top;
            exitDist.Y = staticRect.Top - movingRect.Bottom;
        }

        Vector2 entryTime = new Vector2();
        Vector2 exitTime = new Vector2();

        if (velocity.X == 0)
        {
            entryTime.X = float.NegativeInfinity;
            exitTime.X = float.PositiveInfinity;
        }
        else
        {
            entryTime.X = entryDist.X / velocity.X;
            exitTime.X = exitDist.X / velocity.X;
        }

        if (velocity.Y == 0)
        {
            entryTime.Y = float.NegativeInfinity;
            exitTime.Y = float.PositiveInfinity;
        }
        else
        {
            entryTime.Y = entryDist.Y / velocity.Y;
            exitTime.Y = exitDist.Y / velocity.Y;
        }

        float entry = Math.Max(entryTime.X, entryTime.Y);
        float exit = Math.Min(exitTime.X, exitTime.Y);

        // No collision conditions
        if (entry > exit || entry < 0 || entry > 1)
            return (1f, Vector2.Zero);

        Vector2 normal = Vector2.Zero;
        if (entryTime.X > entryTime.Y)
        {
            normal.X = velocity.X < 0 ? 1 : -1;
        }
        else
        {
            normal.Y = velocity.Y < 0 ? 1 : -1;
        }

        return (entry, normal);
    }

    private void UpdatePosition()
    {
        Vector2 originalPosition = Position;
        Vector2 movement = MovementVector * (float)BlastiaGame.GameTimeElapsedSeconds;
        Vector2 remaining = movement;
        
        int iterations = 0;
        const int maxIterations = 5;
        bool collisionOccurred = false;

        while (iterations < maxIterations && remaining.LengthSquared() > float.Epsilon)
        {
            float shortestTime = 1f;
            Vector2 collisionNormal = Vector2.Zero;
            Rectangle playerRect = GetPlayerRect(Position);
            
            // Expand search area for edge cases
            Rectangle searchArea = Rectangle.Union(
                playerRect,
                new Rectangle(
                    (int)(playerRect.X + remaining.X),
                    (int)(playerRect.Y + remaining.Y),
                    playerRect.Width,
                    playerRect.Height
                )
            );

            int tileStartX = (int)Math.Floor(searchArea.Left / (float)Block.Size) - 1;
            int tileEndX = (int)Math.Floor(searchArea.Right / (float)Block.Size) + 1;
            int tileStartY = (int)Math.Floor(searchArea.Top / (float)Block.Size) - 1;
            int tileEndY = (int)Math.Floor(searchArea.Bottom / (float)Block.Size) + 1;

            // Check all tiles in expanded area
            for (int tx = tileStartX; tx <= tileEndX; tx++)
            {
                for (int ty = tileStartY; ty <= tileEndY; ty++)
                {
                    int tileWorldX = tx * Block.Size;
                    int tileWorldY = ty * Block.Size;
                    
                    if (!PlayerManager.Instance.SelectedWorld.HasTile(tileWorldX, tileWorldY))
                        continue;

                    Rectangle tileRect = new Rectangle(tileWorldX, tileWorldY, Block.Size, Block.Size);
                    var (toi, normal) = SweptAABB(playerRect, remaining, tileRect);

                    if (toi < shortestTime)
                    {
                        shortestTime = toi;
                        collisionNormal = normal;
                        collisionOccurred = true;
                    }
                }
            }

            if (!collisionOccurred)
            {
                Position += remaining;
                break;
            }

            // Apply partial movement
            Position += remaining * shortestTime;
            
            // Calculate remaining movement after collision
            Vector2 projection = Vector2.Dot(remaining, collisionNormal) * collisionNormal;
            remaining -= projection * (1 - shortestTime);
            
            // Apply overlap correction
            const float skinWidth = 0.1f;
            Position += collisionNormal * skinWidth;

            // Adjust velocity based on collision normal
            if (collisionNormal.X != 0) MovementVector = new Vector2(0, MovementVector.Y);
            if (collisionNormal.Y != 0) MovementVector = new Vector2(MovementVector.X, 0);

            iterations++;
        }

        // Final position validation
        if (iterations >= maxIterations)
        {
            Console.WriteLine("Collision resolution reached max iterations");
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