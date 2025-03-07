using NAudio.Wave;

namespace Blastia.Main.Synthesizer;

public class WaveSynthesizer : ISampleProvider
{
    private readonly WaveFormat _waveFormat;
    private double _phase;
    private double _frequency;
    private double _amplitude;
    private WaveType _waveType;
    
    public enum WaveType { Sine, Square, Triangle, Sawtooth }
    
    public WaveFormat WaveFormat => _waveFormat;
    
    public void SetFrequency(double newFrequency) => _frequency = newFrequency;
    public void SetAmplitude(double newAmplitude) => _amplitude = Math.Clamp(newAmplitude, 0.0, 1.0);
    public void SetWaveType(WaveType newWaveType) => _waveType = newWaveType;
    
    public WaveSynthesizer(WaveType type = WaveType.Sine)
    {
        _waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);
        _frequency = 440; // A4 note
        _amplitude = 0.5; // 50% volume
        _waveType = type;
    }
    
    public int Read(float[] buffer, int offset, int count)
    {
        for (int i = 0; i < count; i++)
        {
            buffer[offset + i] = (float) (_amplitude * GenerateSample(_phase));
            
            _phase += 2 * Math.PI * _frequency / _waveFormat.SampleRate;
            if (_phase > Math.PI * 2)
            {
                _phase -= Math.PI * 2;
            }
        }
        
        return count;
    }

    public double GenerateSample(double phase)
    {
        return _waveType switch
        {
            WaveType.Sine => Math.Sin(phase),
            WaveType.Square => Math.Sin(phase) >= 0 ? 1 : -1,
            WaveType.Triangle => 1 - 2 * Math.Abs(((phase / Math.PI) % 2) - 1),
            WaveType.Sawtooth => (phase % (2 * Math.PI)) / Math.PI - 1,
            _ => Math.Sin(phase)
        };
    }
}