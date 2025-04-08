using Microsoft.Xna.Framework;

namespace Blastia.Main.Physics;

public static class Collision
{
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
        float exitTime = Math.Min(yExitTime, yExitTime);
        
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
}