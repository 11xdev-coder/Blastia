using System.Numerics;
using ImGuiNET;
using NAudio.Dsp;
using NAudio.Wave;
using Complex = System.Numerics.Complex;
using MathNet.Numerics.IntegralTransforms;

namespace Blastia.Main.Synthesizer;

/// <summary>
/// Analyzes an audio file and returns basic MusicTrack
/// </summary>
public static class AudioAnalyzer
{
    private static string _filePath = "";
    /// <summary>
    /// Renders <c>ImGui</c> UI
    /// </summary>
    /// <param name="show">Whether to show the popup window</param>
    public static void RenderUi(ref bool show)
    {
        if (!show) return;
        
        ImGui.SetNextWindowSize(new Vector2(1000, 800));
        ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.Appearing);

        if (ImGui.Begin("Analyzer", ref show, ImGuiWindowFlags.NoCollapse))
        {
            ImGui.InputText("File path", ref _filePath, 256);
            
            ImGui.End();
        }
    }
    
    
    /// <summary>
    /// Analyzes audio file located at <c>filePath</c>
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    /// <exception cref="Exception">If audio length is too small (<c>sample count &lt; 1024</c>) or no midi notes were detected, throws an exception</exception>
    public static MusicTrack Analyze(string filePath)
    {
        // read audio samples
        List<float> samples = [];
        int sampleRate = 44100;
        using var reader = new AudioFileReader(filePath);
        
        // one second buffer
        sampleRate = reader.WaveFormat.SampleRate;
        int channels = reader.WaveFormat.Channels;
        int bufferSize = sampleRate * channels;
        float[] buffer = new float[bufferSize];

        int read;
        while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (int i = 0; i < read; i += channels)
            {
                samples.Add(buffer[i]);
            }
        }

        if (samples.Count < 1024)
        {
            throw new Exception("Audio file is too short.");
        }

        int windowSize = 1024;
        int hopSize = 512; // 50% overlap
        int numWindows = (samples.Count - windowSize) / hopSize;

        List<int> midiNotes = [];
        for (int i = 0; i < numWindows; i++)
        {
            int start = i * hopSize;
            Complex[] fftBuffer = new Complex[windowSize];
            for (int j = 0; j < windowSize; j++)
            {
                float sampleValue = samples[start + j];
                fftBuffer[j] = new Complex(sampleValue, 0);
            }
            
            Fourier.Forward(fftBuffer, FourierOptions.Matlab);

            double maxMagnitude = 0;
            int maxIndex = 0;
            for (int k = 0; k < windowSize / 2; k++)
            {
                double mag = fftBuffer[k].Magnitude;
                if (mag > maxMagnitude)
                {
                    maxMagnitude = mag;
                    maxIndex = k;
                }
            }

            double freqResolution = sampleRate / (double)windowSize;
            double dominantFrequency = maxIndex * freqResolution;
            int midi = FrequencyToMidi(dominantFrequency);
            midiNotes.Add(midi);
        }
        
        // group consecutive windows with similar semitones to note events
        List<MusicNote> notes = [];
        if (midiNotes.Count == 0)
        {
            throw new Exception("No midi notes found.");
        }

        int currentNote = midiNotes[0];
        int startWindow = 0;
        for (int i = 1; i < midiNotes.Count; i++)
        {
            // 1 semitone difference
            if (Math.Abs(midiNotes[i] - currentNote) > 1)
            {
                int durationWindow = i - startWindow;
                // assume each window = 1 step
                int durationSteps = durationWindow;
                MusicNote noteEvent = new MusicNote
                {
                    Note = currentNote,
                    Velocity = 100,
                    StartStep = startWindow,
                    Duration = durationSteps,
                    Channel = 0
                };
                notes.Add(noteEvent);
                
                currentNote = midiNotes[i];
                startWindow = i;
            }
        }
        
        // add final note
        int lastDuration = midiNotes.Count - startWindow;
        MusicNote lastNote = new MusicNote
        {
            Note = currentNote,
            Velocity = 100,
            StartStep = startWindow,
            Duration = lastDuration,
            Channel = 0
        };
        notes.Add(lastNote);

        MusicTrack track = new MusicTrack
        {
            Name = $"Analyzed {filePath}",
            Tempo = 100,
            BarCount = Math.Max(1, notes.Last().StartStep + notes.Last().Duration) / 16, // assuming 1 bar = 16 steps
            Key = notes[0].Note % 12,
            IsMinor = false,
        };

        TrackPart trackPart = new TrackPart
        {
            Type = TrackPartType.Arpeggio,
            Channel = 0,
            Program = 38
        };
        trackPart.Notes.AddRange(notes);
        track.Parts.Add(trackPart);

        return track;
    }

    private static int FrequencyToMidi(double freq)
    {
        if (freq <= 0)
        {
            return 0;
        }

        double midi = 69 + 12 * Math.Log(freq / 440, 2);
        return (int)Math.Round(midi);
    }
}