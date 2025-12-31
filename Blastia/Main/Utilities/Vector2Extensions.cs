using Microsoft.Xna.Framework;

namespace Blastia.Main.Utilities;

public static class Vector2Extensions
{
    public static double Magnitude(this Vector2 vector)
    {
        // √x^2 + y^2
        return Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
    }

    public static Vector2 Normalize(this Vector2 vector)
    {
        float length = (float)vector.Magnitude();
        return new Vector2(vector.X / length, vector.Y / length);
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

    /// <summary>
    /// Checks if Vector2 a is larger than Vector2 b
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool Larger(this Vector2 a, Vector2 b)
    {
        return a.X > b.X && a.Y > b.Y;
    }
    
    /// <summary>
    /// Checks if Vector2 a is smaller than Vector2 b
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool Smaller(this Vector2 a, Vector2 b)
    {
        return a.X < b.X && a.Y < b.Y;
    }
}