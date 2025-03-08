using NAudio.Wave;

namespace Blastia.Main.Synthesizer;

public class MultipleWaveSynth(float sampleRate = 44100) : ISampleProvider
{
    private float _sampleRate = sampleRate;
    private double _phase;
    public List<WaveData> Waves = [];
    private Dictionary<int, double> _wavePhases = [];

    public WaveFormat WaveFormat => WaveFormat.CreateIeeeFloatWaveFormat((int)_sampleRate, 1);
    
    public void AddWave(float frequency, float amplitude, WaveType type, bool isEnabled)
    {
        var newData = new WaveData(frequency, amplitude, type)
        {
            IsEnabled = isEnabled
        };
        Waves.Add(newData);
    }
    
    public void UpdateWave(int index, float frequency, float amplitude, WaveType type, bool isEnabled)
    {
        if (index < 0 || index >= Waves.Count) return;
        
        Waves[index].Frequency = frequency;
        Waves[index].Amplitude = amplitude;
        Waves[index].WaveType = type;
        Waves[index].IsEnabled = isEnabled;
    }

    public void RemoveWave(int index)
    {
        if (index < 0 || index >= Waves.Count) return;
        
        Waves.RemoveAt(index);
    }

    public float GenerateSample()
    {
        float sample = 0f;
        
        // sum all enabled waves
        foreach (WaveData wave in Waves.Where(d => d.IsEnabled))
        {
            sample += GenerateWaveSample(_phase * wave.Frequency / 440f, wave.Amplitude, wave.WaveType);
        }
        
        // TODO: Normalize checkbox

        return sample;
    }

    public float GenerateWaveSample(double phase, float amplitude, WaveType type)
    {
        switch (type)
        {
            case WaveType.Sine:
                return (float)(Math.Sin(phase) * amplitude);
                
            case WaveType.Square:
                return (float)(Math.Sign(Math.Sin(phase)) * amplitude);
                
            case WaveType.Triangle:
                return (float)(Math.Asin(Math.Sin(phase)) * (2 / Math.PI) * amplitude);
                
            case WaveType.Sawtooth:
                return (float)((2 * (phase / (2 * Math.PI) - Math.Floor(phase / (2 * Math.PI) + 0.5))) * amplitude);
                
            default:
                return 0f;
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        for (int i = 0; i < count; i++)
        {
            buffer[offset + i] = 0f;
        }
        
        // process each wave
        for (int waveIndex = 0; waveIndex < Waves.Count; waveIndex++)
        {
            var wave = Waves[waveIndex];
            if (!wave.IsEnabled) continue;
            
            // initialize wave phase
            _wavePhases.TryAdd(waveIndex, 0);

            // calculate samples
            for (int i = 0; i < count; i++)
            {
                float sample = 0f;
                sample += GenerateWaveSample(_wavePhases[waveIndex], wave.Amplitude, wave.WaveType);
                
                buffer[offset + i] += sample;
                
                _wavePhases[waveIndex] += Math.PI * 2 * wave.Frequency / _sampleRate;

                while (_wavePhases[waveIndex] >= Math.PI * 2)
                {
                    _wavePhases[waveIndex] -= Math.PI * 2;
                }
            }
        }

        return count;
    }
}