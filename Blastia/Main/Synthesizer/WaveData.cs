namespace Blastia.Main.Synthesizer;

public enum WaveType { Sine, Square, Triangle, Sawtooth }

public class WaveData(float frequency, float amplitude, WaveType waveType)
{
    public EnvelopeGenerator Envelope { get; set; } = new();
    public float Frequency { get; set; } = frequency;
    public float Amplitude { get; set; } = amplitude;
    public WaveType WaveType { get; set; } = waveType;
    public bool IsEnabled { get; set; } = true;

    public WaveData Clone()
    {
        return new WaveData(Frequency, Amplitude, WaveType);
    }
}