using Blastia.Main.Blocks.Common;
using Blastia.Main.Entities.Common;
using Microsoft.Xna.Framework;

namespace Blastia.Main.Physics;

public class CollisionObject(Rectangle box, bool isCollidable, Entity? entity)
{
    public Rectangle Box { get; set; } = box;
    public bool IsCollidable { get; set; } = isCollidable;
    public Entity? Entity { get; set; } = entity;
}

public static class Collision
{
    public static readonly int CellSize = 8 * Block.Size;
    // cell position -> entities in that cell
    public static Dictionary<Vector2, List<CollisionObject>> Cells = new();

    /// <summary>
    /// Calculates collision details between moving AABB and static AABB
    /// </summary>
    /// <param name="staticBox">Rectangle of the static object</param>
    /// <param name="movingBox">Rectangle of the moving object</param>
    /// <param name="velocity">Velocity of moving object</param>
    /// <param name="timeOfImpact">Output time until collision (0 - 1). 1f if no collision</param>
    /// <param name="impactNormal">Normal of the surface hit at impact</param>
    /// <returns>True if collision has occured</returns>
    public static bool SweptAabb(Rectangle staticBox, Rectangle movingBox, Vector2 velocity, out float timeOfImpact, out Vector2 impactNormal)
    {
        timeOfImpact = 1f;
        impactNormal = Vector2.Zero;
        
        // calculate distances for movingBox to hit staticBox
        float xEntryDist, yEntryDist;
        float xExitDist, yExitDist;
        
        if (velocity.X > 0) // moving right
        {
            // entry -> moving right hits static left
            xEntryDist = staticBox.Left - movingBox.Right;
            // exit -> moving left exits static right
            xExitDist = staticBox.Right - movingBox.Left;
        }
        else // moving left
        {
            // entry -> moving left hits static right
            xEntryDist = staticBox.Right - movingBox.Left;
            // exit -> moving right exits static left
            xExitDist = staticBox.Left - movingBox.Right;
        }

        if (velocity.Y > 0) // moving down
        {
            // entry -> moving bottom hits static top
            yEntryDist = staticBox.Top - movingBox.Bottom;
            // exit -> moving top exits static bottom
            yExitDist = staticBox.Bottom - movingBox.Top;
        }
        else // moving up
        {
            // entry -> moving top hits static bottom
            yEntryDist = staticBox.Bottom - movingBox.Top;
            // exit -> moving bottom exits static top
            yExitDist = staticBox.Top - movingBox.Bottom;
        }

        // calculate leaving/entering times
        float xEntryTime, xExitTime;
        float yEntryTime, yExitTime;

        // not moving
        if (velocity.X == 0)
        {
            // separated
            if (movingBox.Right <= staticBox.Left || movingBox.Left >= staticBox.Right)
            {
                // no collision possible
                return false;
            }
            xEntryTime = float.NegativeInfinity; // start overlap
            xExitTime = float.PositiveInfinity; // always overlapping
        }
        else
        {
            xEntryTime = xEntryDist / velocity.X;
            xExitTime = xExitDist / velocity.X;
        }

        if (velocity.Y == 0)
        {
            // separated
            if (movingBox.Top >= staticBox.Bottom || movingBox.Bottom <= staticBox.Top)
            {
                return false;
            }
            yEntryTime = float.NegativeInfinity; // start overlap
            yExitTime = float.PositiveInfinity; // always overlapping
        }
        else
        {
            yEntryTime = yEntryDist / velocity.Y;
            yExitTime = yExitDist / velocity.Y;
        }
        
        // overall collision
        // latest (object overlaps when both axes overlap)
        float entryTime = Math.Max(xEntryTime, yEntryTime);
        // object doesn't overlap when at least one axis doesn't overlap
        float exitTime = Math.Min(xExitTime, yExitTime);
        
        // no collision if:
        // entryTime > exitTime: collision interval is invalid
        // entryTime < 0: happened in the past (but we are checking for new collisions)
        // entryTime > 1: collision will happen after this frame
        if (entryTime >= exitTime || entryTime < 0 || entryTime >= 1)
        {
            return false;
        }

        timeOfImpact = entryTime;
        
        // first axis to have collision (latest entry time)
        if (xEntryTime > yEntryTime)
        {
            // X
            if (velocity.X < 0) // left
            {
                impactNormal = new Vector2(1, 0);
            }
            else // right
            {
                impactNormal = new Vector2(-1, 0);
            }
        }
        else
        {
            if (velocity.Y < 0) // up
            {
                impactNormal = new Vector2(0, 1);
            }
            else
            {
                impactNormal = new Vector2(0, -1);
            }
        }

        // detected collision
        return true;
    }

    /// <summary>
    /// Calculates bounding box that includes starting and ending position. (Broad-phase collision)
    /// </summary>
    /// <param name="box">Bounding box of the entity</param>
    /// <param name="displacement">Intended movement for this iteration</param>
    /// <returns>Enlarged rectangle covering swept area</returns>
    public static Rectangle GetSweptBounds(Rectangle box, Vector2 displacement)
    {
        Rectangle swept = box;
        
        // left movement
        if (displacement.X < 0)
        {
            swept.X += (int) Math.Floor(displacement.X);
        }
        // right movement
        swept.Width += (int) Math.Ceiling(Math.Abs(displacement.X));
        
        // up movement
        if (displacement.Y < 0)
        {
            swept.Y += (int) Math.Floor(displacement.Y);
        }
        // down
        swept.Height += (int) Math.Ceiling(Math.Abs(displacement.Y));

        return swept;
    }

    /// <summary>
    /// Gets all objects in the grid within the specified rectangle
    /// </summary>
    /// <param name="rectangle"></param>
    /// <returns></returns>
    private static List<CollisionObject> GetObjectsInRectangle(Rectangle rectangle)
    {
        var results = new List<CollisionObject>();
        
        // convert bounds to grid cell coords
        var startCellX = (int)Math.Floor(rectangle.Left / (float)CellSize);
        var startCellY = (int)Math.Floor(rectangle.Top / (float)CellSize);
        var endCellX = (int)Math.Ceiling(rectangle.Right / (float)CellSize);
        var endCellY = (int)Math.Ceiling(rectangle.Bottom / (float)CellSize);
        
        // check each cell
        for (var x = startCellX; x < endCellX; x++)
        {
            for (var y = startCellY; y < endCellY; y++)
            {
                var cellPos = new Vector2(x * CellSize, y * CellSize);
            
                // add this cell's bodies to potential colliders
                if (Cells.TryGetValue(cellPos, out var objects))
                {
                    results.AddRange(objects);
                }
            }
        }
        
        return results;
    }

    /// <summary>
    /// Gets all collidable objects in the specified rectangle
    /// </summary>
    /// <param name="bounds"></param>
    /// <returns></returns>
    public static List<Rectangle> GetPotentialCollidersInRectangle(Rectangle bounds)
    {
        var objects = GetObjectsInRectangle(bounds);
        // filter out collidables
        var colliders = objects.Where(b => b.IsCollidable)
            .Select(b => b.Box).ToList();
        return colliders;
    }

    /// <summary>
    /// Gets all non-collidable objects in the specified rectangle
    /// </summary>
    /// <param name="bounds"></param>
    /// <returns></returns>
    public static List<Entity?> GetPotentialEntitiesInRectangle(Rectangle bounds)
    {
        var objects = GetObjectsInRectangle(bounds);
        
        // filter out non-collidables
        var entities = objects.Where(b => !b.IsCollidable)
            .Select(b => b.Entity).ToList();
        return entities;
    }

    public static void ClearGrid()
    {
        Cells.Clear();
    }

    public static void AddObjectToGrid(Rectangle box,  bool isCollidable, Entity? entity = null)
    {
        var collisionObject = new CollisionObject(box, isCollidable, entity);
        
        // Calculate the grid cells this entity overlaps with
        var startCellX = box.Left / CellSize;
        var startCellY = box.Top / CellSize;
        var endCellX = (box.Right - 1) / CellSize;
        var endCellY = (box.Bottom - 1) / CellSize;
        
        // Add entity to each overlapping cell
        for (int x = startCellX; x <= endCellX; x++)
        {
            for (int y = startCellY; y <= endCellY; y++)
            {
                Vector2 cellPos = new Vector2(x * CellSize, y * CellSize);
                
                // initialize list if it doesnt exist
                if (!Cells.TryGetValue(cellPos, out var bodiesInCell))
                {
                    bodiesInCell = [];
                    Cells[cellPos] = bodiesInCell;
                }
                
                bodiesInCell.Add(collisionObject);
            }
        }
    }
}