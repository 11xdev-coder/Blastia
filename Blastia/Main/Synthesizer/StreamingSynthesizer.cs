using NAudio.Wave;

namespace Blastia.Main.Synthesizer;

public class StreamingSynthesizer : ISampleProvider
{
    // Audio format definition
    public WaveFormat WaveFormat => WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

    // Track and note management
    private Dictionary<int, List<NoteEvent>> _noteEvents = new();
    private List<ActiveNote> _activeNotes = new();
    private readonly object _noteLock = new();
    
    // Playback state
    private int _currentSample;
    private bool _isLooping;
    private int _totalSamples;
    private int _totalBars;

    private int _currentStep;
    private int _currentBar;
    private double _samplesPerStep;

    public int CurrentStep
    {
        get => _currentStep;
        set => _currentStep = Math.Max(0, value);
    }

    public int CurrentBar
    {
        get => _currentBar;
        set => _currentBar = Math.Max(0, value);
    }
    
    public void LoadTrack(MusicTrack track, bool looping = false)
    {
        lock (_noteLock)
        {
            _isLooping = looping;
            
            // Reset state
            _currentSample = 0;
            _currentStep = 0;
            _noteEvents.Clear();
            _activeNotes.Clear();
            
            // Calculate samples per step
            _samplesPerStep = WaveFormat.SampleRate * (60.0 / track.Tempo / 4.0);
            
            // Precalculate all note events by start time
            foreach (var part in track.Parts)
            {
                foreach (var note in part.Notes)
                {
                    int startSample = (int)(note.StartStep * _samplesPerStep);
                    int duration = (int)Math.Round(note.Duration * _samplesPerStep);
                    
                    if (!_noteEvents.ContainsKey(startSample))
                        _noteEvents[startSample] = new List<NoteEvent>();
                        
                    _noteEvents[startSample].Add(new NoteEvent { 
                        Note = note.Note,
                        Velocity = note.Velocity / 127.0f,
                        Duration = duration,
                        Channel = part.Channel,
                        Program = part.Program
                    });
                    
                    Console.WriteLine($"Added note: {note.Note}");
                }
            }
            
            // Calculate total sample length
            int totalSteps = track.BarCount * 16; // 16 steps per bar
            _totalSamples = (int)(totalSteps * _samplesPerStep);
            _totalBars = track.BarCount;
            
            Console.WriteLine($"Track loaded: {_noteEvents.Count} note events, {_totalSamples} total samples");
        }
    }
    
    public void Reset()
    {
        lock (_noteLock)
        {
            _currentSample = 0;
            _currentStep = 0;
            _activeNotes.Clear();
            Console.WriteLine("Playback reset to beginning");
        }
    }
    
    public void SetLooping(bool looping)
    {
        _isLooping = looping;
    }
    
    public int Read(float[] buffer, int offset, int count)
    {
        Array.Clear(buffer, offset, count);
    
        lock (_noteLock)
        {
            if (_currentSample >= _totalSamples && _isLooping) Reset();
            
            // process each sample
            for (int i = 0; i < count; i++)
            {
                int absoluteSample = _currentSample + i;

                // Add new notes
                if (_noteEvents.TryGetValue(absoluteSample, out var events))
                {
                    _activeNotes.AddRange(events.Select(n => new ActiveNote
                    {
                        Frequency = MidiNoteToFrequency(n.Note),
                        Amplitude = n.Velocity,
                        Duration = n.Duration,
                        SamplesPlayed = 0,
                        Program = n.Program
                    }));
                }

                // Process active notes
                float sampleSum = 0;
                for (int noteIndex = _activeNotes.Count - 1; noteIndex >= 0; noteIndex--)
                {
                    var note = _activeNotes[noteIndex];
                    note.SamplesPlayed++;

                    if (note.SamplesPlayed >= note.Duration)
                    {
                        _activeNotes.RemoveAt(noteIndex);
                        continue;
                    }

                    float env = CalculateEnvelope(note);
                    sampleSum += SynthesizeNote(note) * env * note.Amplitude;
                }

                buffer[offset + i] = Math.Clamp(sampleSum * 0.2f, -1, 1);
            }

            _currentSample += count;

            var stepsPerBar = 16;
            _currentStep = (int)(_currentSample / _samplesPerStep);
            if (_currentStep >= stepsPerBar)
            {
                _currentStep = 0;
                _currentBar += 1;

                if (_currentBar >= _totalBars)
                {
                    _currentBar = 0;
                }
            }
            
            return count;
        }
    }

    private float SynthesizeNote(ActiveNote note)
    {
        // Calculate phase based on frequency and samples played
        double phase = (note.SamplesPlayed * note.Frequency) / WaveFormat.SampleRate;
        double t = phase % 1.0;
        
        // Select waveform based on program number
        if (note.Program >= 80 && note.Program < 88) // Lead sounds
            return (float)(Math.Sin(2 * Math.PI * t) * 0.5 + Math.Sin(4 * Math.PI * t) * 0.5);
        
        if (note.Program >= 88 && note.Program < 96) // Pad sounds
            return (float)(Math.Sin(2 * Math.PI * t));
        
        if (note.Program >= 32 && note.Program < 40) // Bass sounds
            return (float)(
                Math.Sin(2 * Math.PI * t) * 0.5 + 
                Math.Sign(Math.Sin(2 * Math.PI * t)) * 0.5
            );
        
        // Default
        return (float)Math.Sin(2 * Math.PI * t);
    }

    private float CalculateEnvelope(ActiveNote note)
    {
        float attackTime = 0.01f;
        float decayTime = 0.1f;
        float sustainLevel = 0.7f;
        float releaseTime = 0.3f;
        
        // Adjust envelope parameters based on program
        if (note.Program >= 88 && note.Program < 96) // Pads
        {
            attackTime = 0.4f;
            releaseTime = 0.8f;
        }
        else if (note.Program >= 32 && note.Program < 40) // Bass
        {
            attackTime = 0.005f;
            decayTime = 0.2f;
        }
        
        // Convert times to samples
        int attackSamples = (int)(attackTime * WaveFormat.SampleRate);
        int decaySamples = (int)(decayTime * WaveFormat.SampleRate);
        int releaseSamples = (int)(releaseTime * WaveFormat.SampleRate);
        
        // Calculate envelope position
        if (note.SamplesPlayed < attackSamples)
        {
            return (float)note.SamplesPlayed / attackSamples;
        }
        else if (note.SamplesPlayed < attackSamples + decaySamples)
        {
            float decayPos = (float)(note.SamplesPlayed - attackSamples) / decaySamples;
            return 1.0f - ((1.0f - sustainLevel) * decayPos);
        }
        else if (note.SamplesPlayed < note.Duration - releaseSamples)
        {
            return sustainLevel;
        }
        else
        {
            float releasePos = (float)(note.SamplesPlayed - (note.Duration - releaseSamples)) / releaseSamples;
            return sustainLevel * (1.0f - releasePos);
        }
    }
    
    private static float MidiNoteToFrequency(int midiNote)
    {
        return 440.0f * (float)Math.Pow(2, (midiNote - 69) / 12.0);
    }

    // Internal classes for note management
    public class ActiveNote
    {
        public float Frequency;
        public float Amplitude;
        public int Duration;
        public int SamplesPlayed;
        public int Program;
    }
}