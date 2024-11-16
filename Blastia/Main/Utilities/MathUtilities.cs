using Microsoft.Xna.Framework;

namespace Blastia.Main.Utilities;

public static class MathUtilities
{
    /// <summary>
    /// PingPong lerps from minValue to maxValue, depending on time and duration. Converts result to radians
    /// </summary>
    /// <param name="minValue"></param>
    /// <param name="maxValue"></param>
    /// <param name="time"></param>
    /// <param name="duration"></param>
    /// <returns></returns>
    public static float PingPongLerpRadians(float minValue, float maxValue, float time, float duration)
    {
        var normalizedTime = GetNormalizedTimeForPingPongLerp(time, duration);
        return MathHelper.ToRadians(MathHelper.Lerp(minValue, maxValue, normalizedTime));
    }
    
    public static Color PingPongLerpColor(Color startColor, Color endColor, float time, float duration)
    {
        var normalizedTime = GetNormalizedTimeForPingPongLerp(time, duration);
        return Color.Lerp(startColor, endColor, normalizedTime);
    }

    private static float GetNormalizedTimeForPingPongLerp(float time, float duration)
    {
        float normalizedTime = (time % (2 * duration)) / duration;
        bool isReturning = normalizedTime > 1.0f;
        normalizedTime = isReturning ? 2.0f - normalizedTime : normalizedTime;

        return normalizedTime;
    }

    /// <summary>
    /// Same as (int) Math.Round(value) -> Rounds the float and casts to int
    /// </summary>
    /// <param name="value">Value to round</param>
    /// <returns>Smoothly rounded value</returns>
    public static int SmoothRound(float value)
    {
        return (int) Math.Round(value);
    }

    /// <summary>
    /// Finds distance between point A and point B and squares it
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static double DistanceBetweenTwoPointsSquared(Vector2 a, Vector2 b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        
        var distance = dx * dx + dy * dy;
        return distance;
    }
}