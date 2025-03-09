using NAudio.Wave;

namespace Blastia.Main.Synthesizer;

public class MultipleWaveSynth(float sampleRate = 44100) : ISampleProvider
{
    private float _sampleRate = sampleRate;
    private double _phase;
    public List<WaveData> Waves = [];
    private Dictionary<int, double> _wavePhases = [];

    public WaveFormat WaveFormat => WaveFormat.CreateIeeeFloatWaveFormat((int)_sampleRate, 1);
    public bool UseAntiAliasing { get; set; } = true;
    
    public void AddWave(float frequency, float amplitude, WaveType type, bool isEnabled)
    {
        var newData = new WaveData(frequency, amplitude, type, new EnvelopeGenerator(), new Filter())
        {
            IsEnabled = isEnabled
        };
        Waves.Add(newData);
    }
    
    public void UpdateWave(int index, float frequency, float amplitude, WaveType type, bool isEnabled, EnvelopeGenerator envelope, Filter filter)
    {
        if (index < 0 || index >= Waves.Count) return;
        
        Waves[index].Frequency = frequency;
        Waves[index].Amplitude = amplitude;
        Waves[index].WaveType = type;
        Waves[index].IsEnabled = isEnabled;
        Waves[index].Envelope = envelope;
        Waves[index].Filter = filter;
    }
    
    public void RemoveWave(int index)
    {
        if (index < 0 || index >= Waves.Count) return;
        
        Waves.RemoveAt(index);
    }

    public float GenerateWaveSample(double phase, float frequency, float amplitude, WaveType type)
    {
        phase %= (Math.PI * 2);
        float t = (float) (phase / (Math.PI * 2));
        float result = 0;
        float dt = 1 / _sampleRate; // normalized sample period
        
        switch (type)
        {
            case WaveType.Sine:
                return (float)(Math.Sin(phase) * amplitude);
                
            case WaveType.Square:
                // basic square wave
                result = t < 0.5f ? 1f : -1f;

                if (UseAntiAliasing)
                {
                    // apply PolyBLEP anti-aliasing
                    if (t < dt) // apply smoothing at rising edge (0.0)
                    {
                        result -= PolyBlep(t / dt);
                    }
                    else if (t > 0.5f && t < 0.5f + dt) // else apply smoothing at falling edge (0.5)
                    {
                        result += PolyBlep((t - 0.5f) / dt);
                    }
                }
                return result * amplitude;
                
            case WaveType.Triangle:
                result = t < 0.5f ? 
                    (float)(4.0 * t - 1.0) : 
                    (float)(3.0 - 4.0 * t);
                
                return result * amplitude;
                
            case WaveType.Sawtooth:
                result = (float) (2 * (t - Math.Floor(t + 0.5f)));

                if (UseAntiAliasing)
                {
                    if (t < dt) // apply smoothing at wrap point
                    {
                        result += PolyBlep(t / dt);
                    }
                    else if (t > 1 - dt)
                    {
                        result += PolyBlep((t - 1) / dt);
                    }
                }
                return result * amplitude;
                
            default:
                return 0f;
        }
    }

    private float PolyBlep(float t)
    {
        if (t < 0) return 0;
        if (t > 1) return 0;

        if (t < 0.5f)
        {
            return 2 * t * t;
        }

        return 1 - 2 * (1 - t) * (1 - t);
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
                sample += GenerateWaveSample(_wavePhases[waveIndex], wave.Frequency, wave.Amplitude, wave.WaveType);
                
                float envelopeValue = wave.Envelope.Process();
                sample *= envelopeValue;
                sample = wave.Filter.Process(sample);
                
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
    
    public void NoteOn()
    {
        foreach (var wave in Waves.Where(w => w.IsEnabled))
        {
            wave.Envelope.NoteOn();
        }
    }

    public void NoteOff()
    {
        foreach (var wave in Waves.Where(w => w.IsEnabled))
        {
            wave.Envelope.NoteOff();
        }
    }
}