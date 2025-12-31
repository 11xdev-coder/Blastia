namespace Blastia.Main.Synthesizer.AiGenerator;

public class AutomationCurve
{
    public List<(double time, float value)> Keyframes { get; set; } = [];

    public float Evaluate(double time)
    {
        if (Keyframes.Count == 0)
            return 0f;
        if (time <= Keyframes[0].time)
            return Keyframes[0].value;
        if (time >= Keyframes[^1].time)
            return Keyframes[^1].value;
        
        // find two keyframes time is between
        for (int i = 0; i < Keyframes.Count; i++)
        {
            if (time < Keyframes[i + 1].time)
            {
                double t0 = Keyframes[i].time;
                double t1 = Keyframes[i + 1].time;
                float v0 = Keyframes[i].value;
                float v1 = Keyframes[i + 1].value;

                double fraction = (time - t0) / (t1 - t0);
                return (float) (v0 + fraction * (v1 - v0));
            }
        }

        return 0f;
    }
}