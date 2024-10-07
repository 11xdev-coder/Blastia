using Microsoft.Xna.Framework;

namespace BlasterMaster.Main.Utilities;

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
        // Normalize the time value for the ping-pong effect.
        float normalizedTime = (time % (2 * duration)) / duration;

        // Check if we are in the forward or backward part of the cycle.
        bool isReturning = normalizedTime > 1.0f;

        // Adjust the normalized time to fit the 0 to 1 range in both directions.
        normalizedTime = isReturning ? 2.0f - normalizedTime : normalizedTime;

        // Lerp between minValue and maxValue using the adjusted time.
        return MathHelper.ToRadians(MathHelper.Lerp(minValue, maxValue, normalizedTime));
    }
    
    public static Color PingPongLerpColor(Color startColor, Color endColor, float time, float duration)
    {
        // Normalize the time value for the ping-pong effect.
        float normalizedTime = (time % (2 * duration)) / duration;

        // Check if we are in the forward or backward part of the cycle.
        bool isReturning = normalizedTime > 1.0f;

        // Adjust the normalized time to fit the 0 to 1 range in both directions.
        normalizedTime = isReturning ? 2.0f - normalizedTime : normalizedTime;

        return Color.Lerp(startColor, endColor, normalizedTime);
    }
}