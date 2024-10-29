using Microsoft.Xna.Framework;

namespace Blastia.Main.Utilities;

public static class Vector2Extensions
{
    public static double Magnitude(this Vector2 vector)
    {
        // √x^2 + y^2
        return Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
    }
    
    public static bool CompareToFloat(this Vector2 vector, float a)
    {
        if (vector.X == a && vector.Y == a)
        {
            return true;
        }

        return false;
    }
    
    public static bool BiggerThanFloat(this Vector2 vector, float a)
    {
        if (vector.X > a && vector.Y > a)
        {
            return true;
        }

        return false;
    }
    
    public static bool SmallerThanFloat(this Vector2 vector, float a)
    {
        if (vector.X < a && vector.Y < a)
        {
            return true;
        }

        return false;
    }
}