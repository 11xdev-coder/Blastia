namespace Blastia.Main.Synthesizer;

public enum WaveType { Sine, Square, Triangle, Sawtooth }

public class WaveData(float frequency, float amplitude, WaveType waveType, EnvelopeGenerator envelope)
{
    public EnvelopeGenerator Envelope { get; set; } = envelope;
    public float Frequency { get; set; } = frequency;
    public float Amplitude { get; set; } = amplitude;
    public WaveType WaveType { get; set; } = waveType;
    public bool IsEnabled { get; set; } = true;

    public WaveData Clone()
    {
        var clonedEnvelope = new EnvelopeGenerator()
        {
            AttackTime = Envelope.AttackTime,
            DecayTime = Envelope.DecayTime,
            SustainLevel = Envelope.SustainLevel,
            ReleaseTime = Envelope.ReleaseTime,
        };
        return new WaveData(Frequency, Amplitude, WaveType, clonedEnvelope);
    }
}