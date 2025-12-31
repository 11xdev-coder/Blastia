using Blastia.Main.Synthesizer.AiGenerator;

namespace Blastia.Main.Synthesizer;

public class WaveData(float frequency, float amplitude, WaveType waveType, EnvelopeGenerator envelope, Filter filter)
{
    public EnvelopeGenerator Envelope { get; set; } = envelope;
    public float Frequency { get; set; } = frequency;
    public float Amplitude { get; set; } = amplitude;
    public WaveType WaveType { get; set; } = waveType;
    public Filter Filter { get; set; } = filter;
    public bool IsEnabled { get; set; } = true;

    public WaveData Clone()
    {
        var clonedEnvelope = new EnvelopeGenerator
        {
            AttackTime = Envelope.AttackTime,
            DecayTime = Envelope.DecayTime,
            SustainLevel = Envelope.SustainLevel,
            ReleaseTime = Envelope.ReleaseTime,
        };
        
        var clonedFilter = new Filter
        {
            Cutoff = Filter.Cutoff,
            Resonance = Filter.Resonance,
            Type = Filter.Type
        };
        
        
        return new WaveData(Frequency, Amplitude, WaveType, clonedEnvelope, clonedFilter);
    }
}