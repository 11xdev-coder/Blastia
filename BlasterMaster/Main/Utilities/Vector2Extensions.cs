using Microsoft.Xna.Framework;

namespace BlasterMaster.Main.Utilities;

public static class Vector2Extensions
{
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