﻿using NAudio.Wave;

namespace Blastia.Main.Synthesizer.AiGenerator;

public enum Style
{
    Default,
    Electronic,
    Synthwave
}

public class StreamingSynthesizer : ISampleProvider
{
    // Audio format definition
    public WaveFormat WaveFormat => WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);

    // Track and note management
    private Dictionary<int, List<NoteEvent>> _noteEvents = new();
    private List<ActiveNote> _activeNotes = new();
    private readonly object _noteLock = new();
    
    // Playback state
    private int _currentSample;
    private bool _isLooping;
    private int _totalSamples;
    private int _totalBars;
    private double _samplesPerStep;
    private readonly int _stepsPerBar = 16;
    private double _globalTime;

    private int _currentStep;
    public int CurrentStep
    {
        get => _currentStep;
        set => _currentStep = value;
    }
    private int _currentBar;

    public int CurrentBar
    {
        get => _currentBar;
        set => _currentBar = value;
    }
    public int StepInBar => _currentStep % _stepsPerBar;

    private volatile Style _currentStyle;
    public Style CurrentStyle
    {
        get => _currentStyle;
        set => _currentStyle = value;
    }

    private volatile float _volumeModifier = 1f;
    public float VolumeModifier
    {
        get => _volumeModifier;
        set => _volumeModifier = value;
    }
    
    private volatile float _vibratoRate;
    public float VibratoRate
    {
        get => _vibratoRate;
        set => _vibratoRate = value;
    }
    
    private volatile float _vibratoDepth = 1f;
    public float VibratoDepth
    {
        get => _vibratoDepth;
        set => _vibratoDepth = value;
    }
    
    // delay
    public const float DelayMixDefault = 0.3f;
    public const float DelayFeedbackDefault = 0.5f;
    public const float DelayTimeDefault = 0.3f;
    private float[] _delayBuffer = [];
    private int _delayBufferSize;
    private int _delayWriteIndex;
    private int _delayReadIndex;
    public float DelayMix = DelayMixDefault; // how much of the delayed signal mixed in
    public float DelayFeedback = DelayFeedbackDefault; // how much of the delayed signal is fed back
    public float DelayTime = DelayTimeDefault;
    private int _delaySamples; // delay time (in samples)
    
    // reverb
    public const float ReverbMixDefault = 0.6f;
    public const float ReverbTimeDefault = 0.5f;
    private float[] _reverbBuffer = [];
    private int _reverbBufferSize;
    private int _reverbWriteIndex;
    public float ReverbMix = ReverbMixDefault; // reverb wet/dry mix
    public float ReverbTime = ReverbTimeDefault;
    
    // bit crusher
    public const int BitCrusherReductionFactorDefault = 2;
    public int BitCrusherReductionFactor = BitCrusherReductionFactorDefault;
    
    // distortion
    public const float DistortionDriveDefault = 2;
    public const float DistortionPostGainDefault = 1;
    public float DistortionDrive = DistortionDriveDefault;
    public float DistortionPostGain = DistortionPostGainDefault;

    public StreamingSynthesizer(Style style, float volume, float vibratoRate = 0.5f, float vibratoDepth = 0.1f)
    {
        CurrentStyle = style;
        VolumeModifier = volume;
        VibratoRate = vibratoRate;
        VibratoDepth = vibratoDepth;
    }

    private void InitializeEffects()
    {
        // delay time of 0.3 seconds
        _delaySamples = (int) (DelayTime * WaveFormat.SampleRate);
        _delayBufferSize = _delaySamples;
        _delayBuffer = new float[_delayBufferSize];
        _delayWriteIndex = 0;
        _delayReadIndex = 0;
        
        // reverb time of 3 seconds
        _reverbBufferSize = 3 * WaveFormat.SampleRate;
        _reverbBuffer = new float[_reverbBufferSize];
        _reverbWriteIndex = 0;
    }
    
    public void LoadTrack(MusicTrack track, float reverbMix, float reverbTime, float delayMix, float delayFeedback, float delayTime, 
        int bitCrusherReduction, float distortionDrive, float distortionPostGain, bool looping = false)
    {
        lock (_noteLock)
        {
            _isLooping = looping;
            _totalBars = track.BarCount;

            // calculate timing
            double millisPerStep = 60000f / track.Tempo / 4f;
            _samplesPerStep = millisPerStep / 1000 * WaveFormat.SampleRate;
            
            // Reset state
            _currentSample = 0;
            _currentStep = 0;
            _globalTime = 0;
            _noteEvents.Clear();
            _activeNotes.Clear();
            
            // Precalculate all note events by start time
            foreach (var part in track.Parts)
            {
                // get list of OscillatorData for each part
                var oscillators = part.SynthParams.Oscillators.Where(o => o.IsEnabled)
                    .Select(o => new OscillatorData
                    {
                        WaveType = o.WaveType,
                        Amplitude = o.Amplitude,
                        FrequencyOffset = o.FrequencyOffset
                    }).ToList();
                
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
                        Program = part.Program,
                        Oscillators = oscillators,
                        SynthParams = part.SynthParams
                    });
                }
            }
            
            _totalSamples = (int)(_totalBars * _stepsPerBar * _samplesPerStep);
            
            ReverbMix = reverbMix;
            ReverbTime = reverbTime;
            DelayMix = delayMix;
            DelayFeedback = delayFeedback;
            DelayTime = delayTime;
            BitCrusherReductionFactor = bitCrusherReduction;
            DistortionDrive = distortionDrive;
            DistortionPostGain = distortionPostGain;
            InitializeEffects();
        }
    }
    
    public void Reset()
    {
        lock (_noteLock)
        {
            _currentSample = 0;
            _currentStep = 0;
            _activeNotes.Clear();
        }
    }

    private void AddNote(NoteEvent n)
    {
        _activeNotes.Add(new ActiveNote
        {
            Frequency = MidiNoteToFrequency(n.Note),
            Amplitude = n.Velocity,
            Duration = n.Duration,
            SamplesPlayed = 0,
            Program = n.Program,
            Oscillators = n.Oscillators,
            SynthParams = n.SynthParams
        });
    }
    
    public int Read(float[] buffer, int offset, int count)
    {
        Array.Clear(buffer, offset, count);

        lock (_noteLock)
        {
            if (_currentSample >= _totalSamples && _isLooping) Reset();

            int framesProcessed = 0;
            int frameCount = count / 2;

            while (framesProcessed < frameCount && _currentSample < _totalSamples)
            {
                double deltaTime = 1.0 / WaveFormat.SampleRate;
                _globalTime += deltaTime;
                
                int absoluteSample = _currentSample + framesProcessed;

                // Add new notes
                if (_noteEvents.TryGetValue(absoluteSample, out var events))
                {
                    foreach (var n in events)
                    {
                        if (CurrentStyle == Style.Synthwave)
                        {
                            var existing = _activeNotes.FirstOrDefault(note => note.Program == n.Program);
                            if (existing == null)
                            {
                                AddNote(n);
                            }
                        }
                        else
                        {
                            AddNote(n);
                        }
                    }
                }

                // Process active notes
                float sampleSum = 0;
                for (int noteIndex = _activeNotes.Count - 1; noteIndex >= 0; noteIndex--)
                {
                    var note = _activeNotes[noteIndex];
                    note.SamplesPlayed++;

                    bool removeNote = false;
                    var synthesized = SynthesizeNote(note, ref removeNote) * note.Amplitude;

                    if (removeNote)
                    {
                        _activeNotes.RemoveAt(noteIndex);
                        continue;
                    }
                    
                    sampleSum += synthesized;
                }
                
                // Write stereo frame
                float sample = Math.Clamp(sampleSum * VolumeModifier * 0.2f, -1, 1);
                buffer[offset + framesProcessed * 2] = sample;
                buffer[offset + framesProcessed * 2 + 1] = sample;

                framesProcessed++;
            }

            _currentSample += framesProcessed;

            // Calculate timeline position from actual samples
            _currentStep = (int)(_currentSample / _samplesPerStep);
            _currentBar = _currentStep / _stepsPerBar;

            // Handle looping
            if (_isLooping && _currentSample >= _totalSamples)
            {
                Reset();
            }
            
            return framesProcessed * 2; // Return actual samples processed
        }
    }

    private float SynthesizeNote(ActiveNote note, ref bool removeNote)
    {
        float sample = CurrentStyle switch
        {
            Style.Default => GenerateDefaultSound(note, ref removeNote),
            Style.Electronic => GenerateElectronicSound(note, ref removeNote),
            Style.Synthwave => GenerateSynthwaveSound(note, ref removeNote),
            _ => GenerateDefaultSound(note, ref removeNote)
        };

        // exit early if we need to remove the note
        if (removeNote)
        {
            return 0f;
        }
        
        // process effects
        foreach (var effect in note.SynthParams.Effects)
        {
            switch (effect.Type)
            {
                case EffectType.Reverb:
                    if (note.SynthParams.Automation.ReverbCurve == null)
                    {
                        float reverbSample = ProcessReverb(sample);
                        sample = MixDryAndWetSignals(sample, reverbSample, effect.Amount);
                    }
                    break;
                case EffectType.Delay:
                    if (note.SynthParams.Automation.DelayCurve == null)
                    {
                        float delaySample = ProcessDelay(sample);
                        sample = MixDryAndWetSignals(sample, delaySample, effect.Amount);
                    }
                    break;
                case EffectType.BitCrusher:
                    sample = ProcessBitCrusher(sample, effect.Amount, note);
                    break;
                case EffectType.Distortion:
                    float distortedSample = ProcessDistortion(sample);
                    sample = MixDryAndWetSignals(sample, distortedSample, effect.Amount);
                    break;
            }
        }

        // check if an automation curve is present
        if (note.SynthParams.Automation.ReverbCurve != null)
        {
            var effectiveReverbAmount = note.SynthParams.Automation.ReverbCurve.Evaluate(_globalTime);
            float reverbSample = ProcessReverb(sample);
            sample = MixDryAndWetSignals(sample, reverbSample, effectiveReverbAmount);
        }
        
        if (note.SynthParams.Automation.DelayCurve != null)
        {
            var effectiveDelayAmount = note.SynthParams.Automation.DelayCurve.Evaluate(_globalTime);
            float delaySample = ProcessDelay(sample);
            sample = MixDryAndWetSignals(sample, delaySample, effectiveDelayAmount);
        }

        return sample;
    }

    private float MixDryAndWetSignals(float sample, float affectedSample, float effectAmount)
    {
        return (1 - effectAmount) * sample + effectAmount * affectedSample;
    }
    
    private float GenerateElectronicSound(ActiveNote note, ref bool removeNote)
    {
        // Calculate envelope once using the first oscillator's envelope (if available)
        var oscForEnv = note.SynthParams.Oscillators.FirstOrDefault();
        float env = CalculateEnvelope(note, oscForEnv);
        if (env <= 0.000001f)
        {
            removeNote = true;
            return 0;
        }
        
        float sampleSum = 0f;
        double baseFrequency = note.Frequency;

        foreach (var osc in note.Oscillators)
        {
            double frequency = baseFrequency * Math.Pow(2, osc.FrequencyOffset / 12.0);
            double phase = (note.SamplesPlayed * frequency) / WaveFormat.SampleRate;
            double t = phase % 1.0;

            float sample = osc.WaveType switch
            {
                // Less aggressive wave shaping
                WaveType.Square => ImprovedSquareWave(t),
                WaveType.Sawtooth => BalancedSawtoothWave(t),
                WaveType.Triangle => CrispTriangleWave(t),
                _ => (float)Math.Sin(2 * Math.PI * t)
            };
            
            sampleSum += sample * osc.Amplitude;
        }

        sampleSum *= env;

        // Add subtle saturation instead of heavy filtering
        float saturated = sampleSum / (1 + Math.Abs(sampleSum)); // Soft clipping
        return Math.Clamp(saturated, -1f, 1f); // Increased output volume
    }

    // Waveform helpers with better high-frequency preservation
    private float ImprovedSquareWave(double t)
    {
        // Square wave with gentle slope (5% smoothing)
        return Math.Sign(Math.Sin(2 * Math.PI * t)) * 0.95f;
    }

    private float BalancedSawtoothWave(double t)
    {
        // Band-limited sawtooth using polyBLEP algorithm
        float saw = (float)(2 * t - 1);
        float phase = (float)t % 1.0f;
        
        // PolyBLEP antialiasing
        if (phase < 0.5f)
            saw += (phase * phase * 2 - 0.5f) * 0.2f;
        else
            saw -= ((1 - phase) * (1 - phase) * 2 - 0.5f) * 0.2f;

        return saw;
    }

    private float CrispTriangleWave(double t)
    {
        // Cleaner triangle wave with minimal shaping
        return (float)(1 - 4 * Math.Abs(t - Math.Floor(t + 0.5)));
    }

    private float GenerateSynthwaveSound(ActiveNote note, ref bool removeNote)
    {
        // We'll accumulate samples from each oscillator defined in the SynthParams.
        float sampleSum = 0f;
        double baseFrequency = note.Frequency;
        float totalEnvelope = 0f;
    
        // Use _globalTime to drive a continuously evolving phase.
        // This makes the sound continuous even if note.SamplesPlayed is reset
        foreach (var osc in note.SynthParams.Oscillators)
        {
            // instantiate filter
            if (osc.Filter != null)
            {
                osc.InstantiatedFilter ??= new Filter
                {
                    Cutoff = osc.Filter.Cutoff,
                    Type = osc.Filter.Type,
                    Resonance = osc.Filter.Resonance
                };
            }
            
            var env = CalculateEnvelope(note, osc);
            totalEnvelope += env;
            
            // Calculate effective frequency including detuning (in semitones)
            double frequency = baseFrequency * Math.Pow(2, osc.FrequencyOffset / 12.0);
        
            // apply vibrato
            double pitchModulation = VibratoDepth * Math.Sin(Math.PI * 2 * VibratoRate * note.SamplesPlayed / WaveFormat.SampleRate);
            double modulatedFrequency = frequency * Math.Pow(2, pitchModulation / 12);
            
            // Use global time to generate a continuous phase.
            double phase = (note.SamplesPlayed * modulatedFrequency) / WaveFormat.SampleRate % 1.0;
            float oscSample;
        
            switch (osc.WaveType)
            {
                case WaveType.Sawtooth:
                    oscSample = BalancedSawtoothWave(phase);
                    break;
                case WaveType.Square:
                    oscSample = ImprovedSquareWave(phase);
                    break;
                case WaveType.Triangle:
                    oscSample = CrispTriangleWave(phase);
                    break;
                default:
                    oscSample = (float)Math.Sin(2 * Math.PI * phase);
                    break;
            }
        
            // Multiply by the oscillator's amplitude.
            var sample = oscSample * osc.Amplitude * env;
            if (osc.InstantiatedFilter != null) sample = osc.InstantiatedFilter.Process(sample);

            sampleSum += sample;
        }

        if (totalEnvelope <= 0.000001f)
        {
            removeNote = true;
            return 0f;
        }
        
        // Apply a slow LFO for gentle amplitude modulation.
        float lfo = (float)(0.8 + 0.2 * Math.Sin(2 * Math.PI * 0.2 * _globalTime));
    
        float output = sampleSum * lfo;
        return Math.Clamp(output, -1f, 1f);
    }

    private float GenerateDefaultSound(ActiveNote note, ref bool removeNote)
    {
        var env = CalculateEnvelope(note, null);

        if (env <= 0.001f)
        {
            removeNote = true;
            return 0;
        }
        
        // Calculate phase based on frequency and samples played
        double phase = (note.SamplesPlayed * note.Frequency) / WaveFormat.SampleRate;
        double t = phase % 1.0;
        
        float waveform = (float)Math.Sin(2 * Math.PI * t);
        if (note.Program >= 80 && note.Program < 88) // Lead sounds
            waveform = (float)(Math.Sin(2 * Math.PI * t) * 0.5 + Math.Sin(4 * Math.PI * t) * 0.5);
        else if (note.Program >= 88 && note.Program < 96) // Pad sounds
            waveform = (float)Math.Sin(2 * Math.PI * t);
        else if (note.Program >= 32 && note.Program < 40) // Bass sounds
            waveform = (float)(Math.Sin(2 * Math.PI * t) * 0.5 + Math.Sign(Math.Sin(2 * Math.PI * t)) * 0.5);
        return waveform * env;
    }

    private float CalculateEnvelope(ActiveNote note, WaveParameters? oscillator)
    {
        // Get envelope parameters based on program
        (float attackTime, float decayTime, float sustainLevel, float releaseTime) = GetEnvelopeParams(note.Program, oscillator);
    
        int attackSamples = (int)(attackTime * WaveFormat.SampleRate);
        int decaySamples = (int)(decayTime * WaveFormat.SampleRate);
        int releaseSamples = (int)(releaseTime * WaveFormat.SampleRate);
    
        // Calculate sustain phase duration from note.Duration
        int sustainSamples = Math.Max(0, note.Duration - (attackSamples + decaySamples));
        int totalDuration = attackSamples + decaySamples + sustainSamples + releaseSamples;
    
        if (note.SamplesPlayed >= totalDuration)
            return 0f;
    
        if (note.SamplesPlayed < attackSamples)
            return (float)note.SamplesPlayed / attackSamples;
    
        if (note.SamplesPlayed < attackSamples + decaySamples)
        {
            float decayPos = (float)(note.SamplesPlayed - attackSamples) / decaySamples;
            return 1.0f - ((1.0f - sustainLevel) * decayPos);
        }
    
        if (note.SamplesPlayed < attackSamples + decaySamples + sustainSamples)
        {
            // Sustain phase: hold the sustain level
            return sustainLevel;
        }
    
        // Release phase: ramp down from sustain level to 0
        float releasePos = (float)(note.SamplesPlayed - (attackSamples + decaySamples + sustainSamples)) / releaseSamples;
        return Math.Max(0, sustainLevel * (1.0f - releasePos));
    }
    
    private (float attack, float decay, float sustain, float release) GetEnvelopeParams(int program, WaveParameters? oscillator)
    {
        if (oscillator == null)
        {
            return program switch
            {
                >= 88 and < 96 => (0.4f, 0.2f, 0.7f, 0.8f), // Pads
                >= 32 and < 40 => (0.005f, 0.2f, 0.7f, 0.3f), // Bass
                _ => (0.01f, 0.1f, 0.7f, 0.3f) // Default
            };
        }
        return (oscillator.Envelope.AttackTime, oscillator.Envelope.DecayTime, oscillator.Envelope.SustainLevel, oscillator.Envelope.ReleaseTime);
    }

    private float ProcessDelay(float input)
    {
        float delayedSample = _delayBuffer[_delayReadIndex];
        
        float output = input + DelayMix * delayedSample;
        _delayBuffer[_delayWriteIndex] = input + DelayFeedback * delayedSample;
        
        _delayWriteIndex = (_delayWriteIndex + 1) % _delayBufferSize;
        _delayReadIndex = (_delayReadIndex + 1) % _delayBufferSize;

        return output;
    }

    private float ProcessReverb(float input)
    {
        // very simple reverb
        // use several taps from reverb buffer
        float reverbOutput = 0;
        // taps
        int[] tapDelays =
        [
            (int)(0.3 * WaveFormat.SampleRate * ReverbTime),  // ~300ms delay
            (int)(0.6 * WaveFormat.SampleRate * ReverbTime),  // ~600ms delay
            (int)(0.9 * WaveFormat.SampleRate * ReverbTime),  // ~900ms delay
            (int)(1.2 * WaveFormat.SampleRate * ReverbTime)   // ~1200ms delay
        ];
        
        float[] tapGains = [0.9f, 0.7f, 0.5f, 0.35f];

        for (int i = 0; i < tapDelays.Length; i++)
        {
            int tapIndex = (_reverbWriteIndex - tapDelays[i] + _reverbBufferSize) % _reverbBufferSize;
            reverbOutput += _reverbBuffer[tapIndex] * tapGains[i];
        }

        // write input + a bit of output
        float feedback = 0.5f;
        _reverbBuffer[_reverbWriteIndex] = input + reverbOutput * feedback;
        _reverbWriteIndex = (_reverbWriteIndex + 1) % _reverbBufferSize;
    
        return input + reverbOutput * ReverbMix;
    }

    private float ProcessBitCrusher(float input, float amount, ActiveNote note)
    {
        // reduce max to 4 bits
        int targetBits = (int)(16 - amount * 12); 
        int levels = 1 << targetBits; // 2^targetBits
        float quantized = (float)Math.Round(input * levels) / levels;
    
        // sample rate reduction
        note.BitCrusherCounter += 1;
        if (note.BitCrusherCounter >= BitCrusherReductionFactor)
        {
            note.BitCrusherStoredSample = quantized;
            note.BitCrusherCounter = 0;
        }
    
        return note.BitCrusherStoredSample;
    }

    private float ProcessDistortion(float input)
    {
        // pre-gain: drive input
        float drivenSample = input * DistortionDrive;
        
        // waveshape using hard-clipping
        float distortedSample;
        float threshold = 0.5f;
        if (drivenSample > threshold)
            distortedSample = threshold;
        else if (drivenSample < -threshold)
            distortedSample = -threshold;
        else
            distortedSample = drivenSample;
        
        // post-gain
        float outputSample = distortedSample * DistortionPostGain;
        return outputSample;
    }
    
    private static float MidiNoteToFrequency(int midiNote)
    {
        return 440.0f * (float)Math.Pow(2, (midiNote - 69) / 12.0);
    }
    
    private struct NoteEvent
    {
        public int Note;
        public float Velocity;
        public int Duration;
        public int Channel;
        public int Program;
        public List<OscillatorData> Oscillators;
        public SynthParameters SynthParams;
    }
    
    public struct OscillatorData
    {
        public WaveType WaveType;
        public float Amplitude;
        public float FrequencyOffset;
    }

    public class ActiveNote
    {
        public float Frequency;
        public float Amplitude;
        public int Duration;
        public int SamplesPlayed;
        public int Program;
        public List<OscillatorData> Oscillators = [];
        public SynthParameters SynthParams = new();
        
        // bitcrusher
        public int BitCrusherCounter;
        public float BitCrusherStoredSample;
    }
}