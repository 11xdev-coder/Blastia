using System.Diagnostics;
using System.Numerics;
using System.Text;
using NAudio.Midi;
using ImGuiNET;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Blastia.Main.Synthesizer
{
    public class AiMusicGenerator
    {
        private static Random _rng = new();
        private static MusicTrack? _currentTrack;
        private static List<MusicTrack> _savedTracks = [];
        private static bool _isPlaying;
        private static bool _isLooping;
        private static DateTime _generationStart;
        private static float _generationProgress;

        // Generation parameters
        private static readonly string[] TrackStyles = ["Industrial", "Ambient", "Combat", "Exploration", "Tense"];
        private static int _selectedStyle;
        private static readonly string[] SynthGenerationStyles = ["Default", "Synthwave"];
        private static int _selectedSynthGeneration;
        private static int _tempo = 110;
        private static int _trackLength = 16; // bars
        private static float _complexity = 0.7f;
        private static float _intensity = 0.8f;
        private static bool _includeArpeggios = true;
        private static bool _includePads = true;
        private static bool _includeBass = true;
        private static bool _includePercussion = true;
        private static bool _includeLead = true;
        private static bool _includeGlitch = true;
        private static bool _isGeneratingTrack;
        private static string _currentStatusMessage = "";
        private static string _exportStatus = "Not exporting";
        private static bool _showExportMenu;
        private static int _selectedTrackIndex;
        
        // reverb
        private static bool _includeReverb;
        private static float _reverbMix = StreamingSynthesizer.ReverbMixDefault;
        private static float _reverbTime = StreamingSynthesizer.ReverbTimeDefault;
        
        // delay
        private static bool _includeDelay;
        private static float _delayMix = StreamingSynthesizer.DelayMixDefault;
        private static float _delayFeedback = StreamingSynthesizer.DelayFeedbackDefault;
        private static float _delayTime = StreamingSynthesizer.DelayTimeDefault;
        
        // bit crusher
        private static bool _includeBitCrusher;
        private static int _bitCrusherReduction = StreamingSynthesizer.BitCrusherReductionFactorDefault;
        
        // distortion
        private static bool _includeDistortion;
        private static float _distortionDrive = StreamingSynthesizer.DistortionDriveDefault;
        private static float _distortionPostGain = StreamingSynthesizer.DistortionPostGainDefault;
        
        // Synth provider
        private static int _selectedSynthStyle;
        private static readonly string[] SynthStyles = ["Default", "Electronic - uses synth", "Synthwave - uses synth"];
        
        private static WaveOutEvent? _waveOut;
        private static StreamingSynthesizer? _synth;
        private static bool _isInitialized;
        
        public static void Initialize()
        {
            InitializeAudioSystem();
        }

        private static void RenderCheckboxWithTooltip(string label, ref bool value, string tooltip, bool sameLine = false)
        {
            ImGui.Checkbox(label, ref value);
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(tooltip);
            }
            if (sameLine) ImGui.SameLine();
        }

        private static void RenderSliderFloatWithFallback(string label, ref float value, float min, float max,
            Action<float> onValueChanged)
        {
            if (ImGui.SliderFloat(label, ref value, min, max))
            {
                onValueChanged(value);
            }
        }
        
        private static void RenderSliderIntWithFallback(string label, ref int value, int min, int max,
            Action<int> onValueChanged)
        {
            if (ImGui.SliderInt(label, ref value, min, max))
            {
                onValueChanged(value);
            }
        }

        public static void RenderUi(ref bool show)
        {
            if (!show) return;
            
            ImGui.SetNextWindowSize(new Vector2(1200, 1000));
            ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.Appearing);

            if (ImGui.Begin("AI", ref show, ImGuiWindowFlags.NoCollapse))
            {
                if (ImGui.CollapsingHeader("AI Music Generator"))
                {
                    ImGui.Text("Generate complete music tracks");
                    ImGui.Separator();

                    // Style selection
                    if (ImGui.Combo("Style", ref _selectedStyle, TrackStyles, TrackStyles.Length))
                    {
                        // Adjust defaults based on style
                        switch (_selectedStyle)
                        {
                            case 0: // Industrial
                                _tempo = 110;
                                _complexity = 0.7f;
                                _intensity = 0.8f;
                                _includeArpeggios = true;
                                _includePads = false;
                                _includeGlitch = true;
                                break;
                            case 1: // Ambient
                                _tempo = 90;
                                _complexity = 0.5f;
                                _intensity = 0.4f;
                                _includeBass = false;
                                _includePercussion = true;
                                _includeArpeggios = true;
                                _includePads = false;
                                _includeLead = true;
                                _includeGlitch = true;
                                break;
                            case 2: // Combat
                                _tempo = 125;
                                _complexity = 0.8f;
                                _intensity = 1.0f;
                                _includeArpeggios = true;
                                _includePads = false;
                                _includeGlitch = true;
                                break;
                            case 3: // Exploration
                                _tempo = 100;
                                _complexity = 0.6f;
                                _intensity = 0.5f;
                                _includeArpeggios = true;
                                _includePads = true;
                                _includeGlitch = false;
                                break;
                            case 4: // Tense
                                _tempo = 105;
                                _complexity = 0.9f;
                                _intensity = 0.7f;
                                _includeArpeggios = true;
                                _includePads = true;
                                _includeGlitch = true;
                                break;
                            case 5: // synthwave
                                _tempo = 118;
                                _complexity = 0.6f;
                                _intensity = 0.7f;
                                _includeArpeggios = true;
                                _includePads = true;
                                _includeGlitch = false;
                                _includeBass = true;
                                _includeLead = true;
                                _selectedSynthStyle = 2;
                                break;
                        }
                    }

                    if (ImGui.Combo("Synth Generation Style", ref _selectedSynthGeneration, SynthGenerationStyles,
                            SynthGenerationStyles.Length))
                    {
                        switch (_selectedSynthGeneration)
                        {
                            case 1: // synthwave
                                _selectedSynthStyle = 2;
                                break;
                        }
                    }

                    // Generation parameters
                    ImGui.SliderInt("Tempo", ref _tempo, 40, 140);
                    ImGui.SliderInt("Length (bars)", ref _trackLength, 8, 32);
                    ImGui.SliderFloat("Complexity", ref _complexity, 0.1f, 1.0f);
                    ImGui.SliderFloat("Intensity", ref _intensity, 0.1f, 1.0f);

                    // Track element toggles
                    ImGui.Text("Track Elements:");
                    ImGui.Checkbox("Bass", ref _includeBass); ImGui.SameLine();
                    ImGui.Checkbox("Percussion", ref _includePercussion); ImGui.SameLine();
                    ImGui.Checkbox("Arpeggios", ref _includeArpeggios);
                    ImGui.Checkbox("Pads", ref _includePads); ImGui.SameLine();
                    ImGui.Checkbox("Lead", ref _includeLead); ImGui.SameLine();
                    ImGui.Checkbox("Glitch Effects", ref _includeGlitch);

                    // Effects
                    ImGui.Text("Generate Effects:");
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Effects are generated at track creation and cannot be modified later. Effect settings can be edited post-generating");
                    }

                    RenderCheckboxWithTooltip("Reverb", ref _includeReverb, "Generated for -> Percussion, Arpeggio, Pad, Lead", true);
                    RenderCheckboxWithTooltip("Delay", ref _includeDelay, "Generated for -> Arpeggio, Lead, Glitch", true);
                    RenderCheckboxWithTooltip("Bit crusher", ref _includeBitCrusher, "Generated for -> Bass, Glitch");
                    RenderCheckboxWithTooltip("Distortion", ref _includeDistortion, "Generated for -> Percussion (intensity > 0.7), Lead (intensity > 0.6), Glitch");

                    if (_includeReverb)
                    {
                        if (ImGui.CollapsingHeader("Reverb Settings"))
                        {
                            _reverbMix = _synth?.ReverbMix ?? StreamingSynthesizer.ReverbMixDefault;
                            RenderSliderFloatWithFallback("Reverb Mix", ref _reverbMix, 0.1f, 2f, val =>
                            {
                                if (_synth != null) _synth.ReverbMix = val;
                            });
                            
                            _reverbTime = _synth?.ReverbTime ?? StreamingSynthesizer.ReverbTimeDefault;
                            RenderSliderFloatWithFallback("Reverb Time", ref _reverbTime, 0f, 7f, val =>
                            {
                                if (_synth != null) _synth.ReverbTime = val;
                            });
                        }
                    }
                    
                    if (_includeDelay)
                    {
                        if (ImGui.CollapsingHeader("Delay Settings"))
                        {
                            _delayMix = _synth?.DelayMix ?? StreamingSynthesizer.DelayMixDefault;
                            RenderSliderFloatWithFallback("Delay Mix", ref _delayMix, 0.1f, 2f, val =>
                            {
                                if (_synth != null) _synth.DelayMix = val;
                            });
                            _delayFeedback = _synth?.DelayFeedback ?? StreamingSynthesizer.DelayFeedbackDefault;
                            RenderSliderFloatWithFallback("Delay Feedback", ref _delayFeedback, 0f, 1f, val =>
                            {
                                if (_synth != null) _synth.DelayFeedback = val;
                            });
                            _delayTime = _synth?.DelayTime ?? StreamingSynthesizer.DelayTimeDefault;
                            RenderSliderFloatWithFallback("Delay Time", ref _delayTime, 0f, 7f, val =>
                            {
                                if (_synth != null) _synth.DelayTime = val;
                            });
                        }
                    }

                    if (_includeBitCrusher)
                    {
                        if (ImGui.CollapsingHeader("Bit Crusher Settings"))
                        {
                            _bitCrusherReduction = _synth?.BitCrusherReductionFactor ?? StreamingSynthesizer.BitCrusherReductionFactorDefault;
                            RenderSliderIntWithFallback("Sample Reduction", ref _bitCrusherReduction, 0, 14, val =>
                            {
                                if (_synth != null) _synth.BitCrusherReductionFactor = val;
                            });
                        }
                    }

                    if (_includeDistortion)
                    {
                        if (ImGui.CollapsingHeader("Distortion Settings"))
                        {
                            _distortionDrive = _synth?.DistortionDrive ?? StreamingSynthesizer.DistortionDriveDefault;
                            RenderSliderFloatWithFallback("Drive", ref _distortionDrive, 0f, 10f, val =>
                            {
                                if (_synth != null) _synth.DistortionDrive = val;
                            });
                            _distortionPostGain = _synth?.DistortionPostGain ?? StreamingSynthesizer.DistortionPostGainDefault;
                            RenderSliderFloatWithFallback("Post Gain", ref _distortionPostGain, 0f, 3f, val =>
                            {
                                if (_synth != null) _synth.DistortionPostGain = val;
                            });
                        }
                    }
                    
                    // synthesizer
                    if (ImGui.Combo("Synthesizer Style", ref _selectedSynthStyle, SynthStyles, SynthStyles.Length))
                    {
                        if (_synth == null) return;
                        
                        _synth.CurrentStyle = (Style) _selectedSynthStyle;
                    }

                    ImGui.Separator();

                    // Generate button
                    if (!_isGeneratingTrack)
                    {
                        if (ImGui.Button("Generate Track", new Vector2(150, 30)))
                        {
                            StartTrackGeneration();
                        }

                        ImGui.SameLine();

                        if (_currentTrack != null)
                        {
                            if (!_isPlaying)
                            {
                                if (ImGui.Button("Play Track", new Vector2(120, 30)))
                                {
                                    StartPlayback();
                                }
                            }
                            else
                            {
                                if (ImGui.Button("Stop", new Vector2(120, 30)))
                                {
                                    StopPlayback();
                                }
                            }

                            ImGui.SameLine();

                            var isLooping = _isLooping;
                            if (ImGui.Checkbox("Is Looping", ref isLooping))
                            {
                                _isLooping = isLooping;
                            }
                            
                            // if (ImGui.Button("Apply to Synth", new Vector2(120, 30)))
                            // {
                            //     ApplyToSynth();
                            // }
                        }
                    }
                    else
                    {
                        // Show progress
                        ImGui.Button("Generating...", new Vector2(150, 30));
                        ImGui.ProgressBar(_generationProgress, new Vector2(-1, 0));
                        ImGui.Text(_currentStatusMessage);
                    }

                    ImGui.Separator();

                    // Display current track info
                    if (_currentTrack != null)
                    {
                        ImGui.Text($"Current Track: {_currentTrack.Name}");
                        ImGui.Text($"Style: {TrackStyles[_currentTrack.Style]}, Tempo: {_currentTrack.Tempo}, {_currentTrack.BarCount} bars");
                        
                        // Show parts
                        ImGui.Text("Parts:");
                        foreach (var part in _currentTrack.Parts)
                        {
                            ImGui.BulletText($"{part.Type}: {part.Notes.Count} notes, {part.Patterns.Count} patterns");
                        }

                        // Track visualization
                        RenderTrackVisualization();
                    }

                    ImGui.Separator();

                    // Saved tracks
                    if (_savedTracks.Count > 0)
                    {
                        ImGui.Text(_exportStatus);
                        ImGui.Text("Saved Tracks:");
                        if (ImGui.BeginTable("savedTracks", 3, ImGuiTableFlags.Borders))
                        {
                            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                            ImGui.TableSetupColumn("Style", ImGuiTableColumnFlags.WidthFixed, 100);
                            ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 200);
                            ImGui.TableHeadersRow();

                            for (int i = 0; i < _savedTracks.Count; i++)
                            {
                                var track = _savedTracks[i];
                                
                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.Text(track.Name);
                                
                                ImGui.TableNextColumn();
                                ImGui.Text(TrackStyles[track.Style]);
                                
                                ImGui.TableNextColumn();
                                if (ImGui.Button($"Load##load{i}", new Vector2(60, 20)))
                                {
                                    _currentTrack = track;
                                }
                                
                                ImGui.SameLine();
                                
                                if (ImGui.Button($"Export##export{i}", new Vector2(60, 20)))
                                {
                                    _selectedTrackIndex = i;
                                    _showExportMenu = !_showExportMenu;
                                }
                                
                                ImGui.SameLine();
                                
                                if (ImGui.Button($"Delete##delete{i}", new Vector2(60, 20)))
                                {
                                    _savedTracks.RemoveAt(i);
                                    i--;
                                }
                            }

                            ImGui.EndTable();
                        }
                    }

                    if (_showExportMenu)
                    {
                        ImGui.SetNextWindowSize(new Vector2(180, 100));
                        ImGui.SetNextWindowPos(ImGui.GetMousePos(), ImGuiCond.Appearing);

                        if (ImGui.Begin("Export", ref _showExportMenu, ImGuiWindowFlags.Modal))
                        {
                            ImGui.Text($"Selected track index: {_selectedTrackIndex}");
                            
                            if (ImGui.Button("MP3##mp3", new Vector2(40, 20)))
                            {
                                ExportTrackToAudio(_savedTracks[_selectedTrackIndex]);
                            }
                            ImGui.SameLine();
                            if (ImGui.Button("WAV##wav", new Vector2(40, 20)))
                            {
                                ExportTrackToAudio(_savedTracks[_selectedTrackIndex], "wav");
                            }
                        }
                        ImGui.End();
                    }
                }
                
                ImGui.End();
            }
        }

        private static void StartTrackGeneration()
        {
            _isGeneratingTrack = true;
            _generationProgress = 0f;
            _currentStatusMessage = "Initializing...";
            _generationStart = DateTime.Now;

            // Start generation in a separate thread
            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    GenerateTrack();
                }
                catch (Exception ex)
                {
                    _currentStatusMessage = $"Error: {ex.Message}";
                    Console.WriteLine($"Track generation error: {ex}");
                }
                finally
                {
                    _isGeneratingTrack = false;
                }
            });
        }

        private static void GenerateTrack()
        {
            UpdateStatus("Creating new track...", 0.05f);

            // Create new track
            MusicTrack track = new MusicTrack
            {
                Name = $"{TrackStyles[_selectedStyle]} Track {DateTime.Now.ToString("yyyyMMdd-HHmmss")}",
                Style = _selectedStyle,
                Tempo = _tempo,
                BarCount = _trackLength
            };

            UpdateStatus("Selecting musical key...", 0.1f);
            track.Key = _rng.Next(12); // 0 = C, 1 = C#, etc.
            track.IsMinor = _rng.NextDouble() < 0.8;

            // Generate chord progression
            UpdateStatus("Generating chord progression...", 0.15f);
            GenerateChordProgression(track);

            // Generate parts
            if (_includeBass)
            {
                UpdateStatus("Generating bass part...", 0.2f);
                track.Parts.Add(GenerateBassLine(track));
            }

            if (_includePercussion)
            {
                UpdateStatus("Generating percussion...", 0.3f);
                track.Parts.Add(GeneratePercussion(track));
            }

            if (_includeArpeggios)
            {
                UpdateStatus("Generating arpeggios...", 0.45f);
                track.Parts.Add(GenerateArpeggio(track));
            }

            if (_includePads)
            {
                UpdateStatus("Generating pads...", 0.6f);
                track.Parts.Add(GeneratePads(track));
            }

            if (_includeLead)
            {
                UpdateStatus("Generating lead...", 0.75f);
                track.Parts.Add(GenerateLead(track));
            }

            if (_includeGlitch)
            {
                UpdateStatus("Adding glitch effects...", 0.85f);
                AddGlitchEffects(track);
            }

            // Generate synth parameters for each part
            UpdateStatus("Creating synth parameters...", 0.9f);
            GenerateSynthParameters(track);

            // Add to saved tracks
            _savedTracks.Insert(0, track);
            if (_savedTracks.Count > 10)
            {
                _savedTracks.RemoveAt(_savedTracks.Count - 1);
            }

            _currentTrack = track;
            UpdateStatus("Track generation complete!", 1.0f);
        }

        private static void UpdateStatus(string message, float progress)
        {
            _currentStatusMessage = message;
            _generationProgress = progress;
            
            // Add a small delay to make UI updates visible
            Thread.Sleep(200);
        }

        private static void GenerateChordProgression(MusicTrack track)
        {
            List<int[]> chordProgression = [];
            
            // Determine scale degrees based on key type
            int[] minorScaleDegrees = { 0, 2, 3, 5, 7, 8, 10 }; // Natural minor scale
            int[] majorScaleDegrees = { 0, 2, 4, 5, 7, 9, 11 }; // Major scale
            
            int[] scaleDegrees = track.IsMinor ? minorScaleDegrees : majorScaleDegrees;
            
            List<int[]> progressionTemplates = [];
            
            if (track.IsMinor)
            {
                // Minor key progressions (using scale degrees)
                progressionTemplates.Add([0, 5, 3, 4]);     // i-VI-iv-V
                progressionTemplates.Add([0, 5, 7, 4]);     // i-VI-VII-V
                progressionTemplates.Add([0, 3, 4, 0]);     // i-iv-v-i
                progressionTemplates.Add([0, 2, 3, 4]);     // i-III-iv-v
                progressionTemplates.Add([0, 7, 3, 4]);     // i-VII-iv-v
                progressionTemplates.Add([0, 5, 0, 4]);     // i-VI-i-v
            }
            else
            {
                progressionTemplates.Add([0, 5, 3, 4]);     // I-vi-IV-V
                progressionTemplates.Add([0, 3, 4, 0]);     // I-IV-V-I
                progressionTemplates.Add([0, 5, 2, 4]);     // I-vi-iii-V
                progressionTemplates.Add([0, 2, 3, 4]);     // I-iii-IV-V
            }
            
            // Select a progression template
            int[] selectedProgression = progressionTemplates[_rng.Next(progressionTemplates.Count)];
            
            // Create the actual chord progression
            foreach (int scaleDegreeIndex in selectedProgression)
            {
                int rootNote = track.Key + scaleDegrees[scaleDegreeIndex % 7];
                int thirdInterval = (scaleDegreeIndex == 0 || scaleDegreeIndex == 2 || scaleDegreeIndex == 5) ? 
                    (track.IsMinor ? 3 : 4) : (track.IsMinor ? 4 : 3);
                int fifthInterval = 7;
                
                // Create chord (root, third, fifth)
                int[] chord =
                [
                    rootNote % 12,
                    (rootNote + thirdInterval) % 12,
                    (rootNote + fifthInterval) % 12
                ];
                
                chordProgression.Add(chord);
            }
            
            // Store the progression in the track
            track.ChordProgression = chordProgression;
            
            // Set number of bars per chord based on track length
            if (track.BarCount >= 16)
            {
                track.BarsPerChord = 4; // 4 bars per chord for longer tracks
            }
            else
            {
                track.BarsPerChord = 2; // 2 bars per chord for shorter tracks
            }
        }

        private static TrackPart GenerateBassLine(MusicTrack track)
        {
            TrackPart bassLine = new TrackPart
            {
                Type = TrackPartType.Bass,
                Channel = 0,
                Program = 38 // Synth Bass 1
            };
            
            int patternLength = 4; // bars
            
            // Calculate steps per bar (16th notes)
            int stepsPerBar = 16;
            int totalSteps = patternLength * stepsPerBar;
            
            // Create bass patterns
            List<BassPattern> patterns = new List<BassPattern>();
            
            // Base pattern - focus on root notes
            BassPattern basePattern = new BassPattern
            {
                Steps = new int[totalSteps],
                Velocities = new int[totalSteps],
                Durations = new int[totalSteps]
            };
            
            // Initialize all to -1 (no note)
            for (int i = 0; i < totalSteps; i++)
            {
                basePattern.Steps[i] = -1;
                basePattern.Velocities[i] = 0;
                basePattern.Durations[i] = 0;
            }
            
            for (int bar = 0; bar < patternLength; bar++)
            {
                int chordIndex = (bar / track.BarsPerChord) % track.ChordProgression.Count;
                int rootNote = track.ChordProgression[chordIndex][0];
                
                // Calculate actual MIDI note (C1 = 36, so start at 36 for bass)
                int bassNote = 36 + rootNote;
                
                // Main rhythm - different patterns based on intensity
                if (_intensity < 0.4)
                {
                    // Simpler pattern for low intensity
                    for (int beat = 0; beat < 4; beat++)
                    {
                        int step = bar * stepsPerBar + beat * 4;
                        if (beat == 0 || beat == 2) // Beats 1 and 3
                        {
                            basePattern.Steps[step] = bassNote;
                            basePattern.Velocities[step] = 100;
                            basePattern.Durations[step] = 4; // Quarter note
                        }
                    }
                }
                else if (_intensity < 0.7)
                {
                    // Medium intensity
                    for (int beat = 0; beat < 4; beat++)
                    {
                        int step = bar * stepsPerBar + beat * 4;
                        basePattern.Steps[step] = bassNote;
                        basePattern.Velocities[step] = beat % 2 == 0 ? 100 : 80;
                        basePattern.Durations[step] = 3; // Slightly detached
                        
                        // Add offbeat note occasionally
                        if (beat % 2 == 1 && _rng.NextDouble() < 0.3)
                        {
                            basePattern.Steps[step + 2] = bassNote;
                            basePattern.Velocities[step + 2] = 70;
                            basePattern.Durations[step + 2] = 2;
                        }
                    }
                }
                else
                {
                    // High intensity - more driving rhythm
                    for (int beat = 0; beat < 4; beat++)
                    {
                        int step = bar * stepsPerBar + beat * 4;
                        basePattern.Steps[step] = bassNote;
                        basePattern.Velocities[step] = 100;
                        basePattern.Durations[step] = 3;
                        
                        // 8th notes
                        if (_rng.NextDouble() < 0.8)
                        {
                            basePattern.Steps[step + 2] = bassNote;
                            basePattern.Velocities[step + 2] = 90;
                            basePattern.Durations[step + 2] = 2;
                        }
                        
                        // Sometimes add 16th notes for extra drive
                        if (_complexity > 0.6 && _rng.NextDouble() < 0.4)
                        {
                            basePattern.Steps[step + 3] = bassNote;
                            basePattern.Velocities[step + 3] = 80;
                            basePattern.Durations[step + 3] = 1;
                        }
                    }
                }
                
                // Sometimes add a fifth or octave for variation
                if (_complexity > 0.5 && _rng.NextDouble() < 0.3)
                {
                    int variationBeat = _rng.Next(4);
                    int variationStep = bar * stepsPerBar + variationBeat * 4;
                    
                    // Use fifth or octave
                    int interval = _rng.NextDouble() < 0.6 ? 7 : 12;
                    basePattern.Steps[variationStep] = bassNote + interval;
                }
            }
            
            patterns.Add(basePattern);
            
            // Create variation patterns
            int numVariations = (int)(_complexity * 2) + 1;
            for (int v = 0; v < numVariations; v++)
            {
                BassPattern variation = CloneBassPattern(basePattern);
                
                // Modify the variation
                for (int i = 0; i < totalSteps; i++)
                {
                    // Sometimes remove notes for rhythmic variation
                    if (variation.Steps[i] != -1 && _rng.NextDouble() < 0.1)
                    {
                        variation.Steps[i] = -1;
                    }
                    
                    // Sometimes add octave jumps
                    if (variation.Steps[i] != -1 && _rng.NextDouble() < 0.15)
                    {
                        variation.Steps[i] += 12;
                    }
                    
                    // Vary velocities
                    if (variation.Steps[i] != -1)
                    {
                        variation.Velocities[i] = Math.Clamp(
                            variation.Velocities[i] + _rng.Next(-10, 11), 60, 127);
                    }
                }
                
                patterns.Add(variation);
            }
            
            // Assign patterns to bassline
            bassLine.Patterns = patterns.Cast<Pattern>().ToList();
            
            // Generate notes from patterns for the full track length
            GenerateNotesFromPatterns(bassLine, track.BarCount, stepsPerBar);
            
            return bassLine;
        }

        private static BassPattern CloneBassPattern(BassPattern original)
        {
            BassPattern clone = new BassPattern
            {
                Steps = (int[])original.Steps.Clone(),
                Velocities = (int[])original.Velocities.Clone(),
                Durations = (int[])original.Durations.Clone()
            };
            return clone;
        }

        private static TrackPart GeneratePercussion(MusicTrack track)
        {
            TrackPart percussion = new TrackPart
            {
                Type = TrackPartType.Percussion,
                Channel = 9, // MIDI channel 10 (9 zero-indexed) for percussion
                Program = 0  // Program doesn't matter for percussion channel
            };
            
            // Create different percussion patterns
            int patternLength = 4; // bars
            int stepsPerBar = 16;  // 16th notes
            int totalSteps = patternLength * stepsPerBar;
            
            PercussionPattern mainPattern = new PercussionPattern
            {
                KickSteps = new bool[totalSteps],
                SnareSteps = new bool[totalSteps],
                HihatSteps = new bool[totalSteps],
                CrashSteps = new bool[totalSteps],
                TomSteps = new bool[totalSteps],
                PercussionSteps = new bool[totalSteps],
                Velocities = new int[totalSteps, 6] // One for each percussion element
            };
            
            // Generate different patterns based on style and intensity
            if (_selectedStyle == 1) // Ambient
            {
                GenerateAmbientPercussion(mainPattern, totalSteps, stepsPerBar);
            }
            else if (_selectedStyle == 2) // Combat
            {
                GenerateCombatPercussion(mainPattern, totalSteps, stepsPerBar);
            }
            else
            {
                // Default pattern (industrial or others)
                GenerateIndustrialPercussion(mainPattern, totalSteps, stepsPerBar);
            }
            
            // Create variations
            List<PercussionPattern> patterns = new List<PercussionPattern> { mainPattern };
            
            // Fill pattern with variations, fills, etc.
            int numVariations = (int)(_complexity * 3) + 1;
            for (int v = 0; v < numVariations; v++)
            {
                PercussionPattern variation = ClonePercussionPattern(mainPattern);
                
                // Add variations specific to each style
                if (_selectedStyle == 2) // Combat - more intense variations
                {
                    AddCombatVariations(variation, totalSteps, stepsPerBar);
                }
                else if (_selectedStyle == 1) // Ambient - subtle variations
                {
                    AddAmbientVariations(variation, totalSteps, stepsPerBar);
                }
                else
                {
                    // Default industrial variations
                    AddIndustrialVariations(variation, totalSteps, stepsPerBar);
                }
                
                patterns.Add(variation);
            }
            
            // Add a fill pattern
            PercussionPattern fillPattern = ClonePercussionPattern(mainPattern);
            AddPercussionFill(fillPattern, totalSteps, stepsPerBar);
            patterns.Add(fillPattern);
            
            // Assign patterns to percussion part
            percussion.Patterns = patterns.Cast<Pattern>().ToList();
            
            // Generate notes
            GeneratePercussionNotes(percussion, patterns, track.BarCount, stepsPerBar);
            
            return percussion;
        }

        private static void GenerateIndustrialPercussion(PercussionPattern pattern, int totalSteps, int stepsPerBar)
        {
            for (int step = 0; step < totalSteps; step++)
            {
                int barStep = step % stepsPerBar;
                
                // Kick drum (36)
                if (barStep == 0 || barStep == 8) // Beats 1 and 3
                {
                    pattern.KickSteps[step] = true;
                    pattern.Velocities[step, 0] = 120; // Hard kick
                }
                else if ((barStep == 4 || barStep == 12) && _intensity > 0.6 && _rng.NextDouble() < 0.3)
                {
                    // Sometimes kick on 2 and 4 for higher intensity
                    pattern.KickSteps[step] = true;
                    pattern.Velocities[step, 0] = 100;
                }
                
                // Snare (38) or Clap (39) on 2 and 4
                if (barStep == 4 || barStep == 12)
                {
                    pattern.SnareSteps[step] = true;
                    pattern.Velocities[step, 1] = 110;
                }
                
                // Hi-hats - 8th notes with accents
                if (barStep % 2 == 0) // 8th notes
                {
                    pattern.HihatSteps[step] = true;
                    pattern.Velocities[step, 2] = (barStep % 8 == 0) ? 100 : 80; // Accent on main beats
                }
                
                // Crash on bar starts occasionally
                if (barStep == 0 && step > 0 && step % (stepsPerBar * 2) == 0)
                {
                    pattern.CrashSteps[step] = true;
                    pattern.Velocities[step, 3] = 110;
                }
                
                // Extra percussion for flavor
                if (_complexity > 0.5 && barStep % 3 == 2 && _rng.NextDouble() < 0.3)
                {
                    pattern.PercussionSteps[step] = true;
                    pattern.Velocities[step, 5] = 90;
                }
            }
        }

        private static void GenerateAmbientPercussion(PercussionPattern pattern, int totalSteps, int stepsPerBar)
        {
            // Ambient style - more sparse, atmospheric percussion
            for (int step = 0; step < totalSteps; step++)
            {
                int barStep = step % stepsPerBar;
                
                // Minimal kick drum
                if (barStep == 0 && step % (stepsPerBar * 2) == 0) // Only on main downbeats
                {
                    pattern.KickSteps[step] = true;
                    pattern.Velocities[step, 0] = 100;
                }
                
                // Sparse snare/clap
                if (barStep == 12 && _rng.NextDouble() < 0.7)
                {
                    pattern.SnareSteps[step] = true;
                    pattern.Velocities[step, 1] = 90;
                }
                
                // Ride cymbal or hi-hat for atmosphere
                if ((barStep % 6) == 0 && _rng.NextDouble() < 0.6)
                {
                    pattern.HihatSteps[step] = true;
                    pattern.Velocities[step, 2] = 70 + _rng.Next(20);
                }
                
                // Toms for tonal percussion
                if (barStep == 10 && _rng.NextDouble() < 0.4)
                {
                    pattern.TomSteps[step] = true;
                    pattern.Velocities[step, 4] = 85;
                }
                
                // Atmospheric percussion
                if (_rng.NextDouble() < 0.1 && barStep % 4 == 3)
                {
                    pattern.PercussionSteps[step] = true;
                    pattern.Velocities[step, 5] = 70 + _rng.Next(30);
                }
            }
        }

        private static void GenerateCombatPercussion(PercussionPattern pattern, int totalSteps, int stepsPerBar)
        {
            // Combat style - aggressive, driving percussion
            for (int step = 0; step < totalSteps; step++)
            {
                int barStep = step % stepsPerBar;
                
                // Heavy kick drum pattern
                if (barStep == 0 || barStep == 4 || barStep == 8 || barStep == 12) // All quarter notes
                {
                    pattern.KickSteps[step] = true;
                    pattern.Velocities[step, 0] = 120;
                }
                else if ((barStep == 2 || barStep == 6 || barStep == 10 || barStep == 14) && _rng.NextDouble() < 0.6)
                {
                    // Additional 8th notes for driving feel
                    pattern.KickSteps[step] = true;
                    pattern.Velocities[step, 0] = 100;
                }
                
                // Snare on 2 and 4 with ghost notes
                if (barStep == 4 || barStep == 12)
                {
                    pattern.SnareSteps[step] = true;
                    pattern.Velocities[step, 1] = 115;
                }
                else if ((barStep == 6 || barStep == 14) && _rng.NextDouble() < 0.4)
                {
                    // Ghost notes
                    pattern.SnareSteps[step] = true;
                    pattern.Velocities[step, 1] = 70;
                }
                
                // Driving hi-hats - 16th notes
                pattern.HihatSteps[step] = true;
                pattern.Velocities[step, 2] = (barStep % 4 == 0) ? 100 : 
                                             (barStep % 2 == 0) ? 90 : 70;
                
                // Crash on major transitions
                if (barStep == 0 && step % (stepsPerBar * 2) == 0)
                {
                    pattern.CrashSteps[step] = true;
                    pattern.Velocities[step, 3] = 120;
                }
                
                // Toms for fills
                if ((step >= totalSteps - stepsPerBar) && barStep % 4 == 3 && _rng.NextDouble() < 0.7)
                {
                    pattern.TomSteps[step] = true;
                    pattern.Velocities[step, 4] = 100;
                }
            }
        }

        private static void AddIndustrialVariations(PercussionPattern pattern, int totalSteps, int stepsPerBar)
        {
            // Add industrial-style variations
            for (int step = 0; step < totalSteps; step++)
            {
                int barStep = step % stepsPerBar;
                
                // Kick drum variations
                if (pattern.KickSteps[step] && _rng.NextDouble() < 0.15)
                {
                    pattern.KickSteps[step] = false; // Remove some kicks
                }
                else if (!pattern.KickSteps[step] && barStep % 4 == 2 && _rng.NextDouble() < 0.2)
                {
                    pattern.KickSteps[step] = true; // Add some syncopation
                    pattern.Velocities[step, 0] = 90;
                }
                
                // Snare variations
                if (pattern.SnareSteps[step] && barStep != 4 && barStep != 12 && _rng.NextDouble() < 0.2)
                {
                    pattern.SnareSteps[step] = false; // Remove some ghost snares
                }
                
                // Add industrial "metal" percussion
                if (_rng.NextDouble() < 0.1 && barStep % 3 == 1)
                {
                    pattern.PercussionSteps[step] = true;
                    pattern.Velocities[step, 5] = 80 + _rng.Next(40);
                }
                
                // Add glitchy hihat pattern in certain places
                if ((step >= totalSteps - stepsPerBar * 2) && _rng.NextDouble() < 0.3)
                {
                    // Randomize hihat pattern for last 2 bars
                    pattern.HihatSteps[step] = _rng.NextDouble() < 0.6;
                    if (pattern.HihatSteps[step])
                    {
                        pattern.Velocities[step, 2] = 70 + _rng.Next(50);
                    }
                }
            }
        }

        private static void AddAmbientVariations(PercussionPattern pattern, int totalSteps, int stepsPerBar)
        {
            // Add subtle ambient variations
            for (int step = 0; step < totalSteps; step++)
            {
                int barStep = step % stepsPerBar;
                
                // Add atmospheric ride hits
                if (barStep % 3 == 0 && _rng.NextDouble() < 0.4)
                {
                    pattern.HihatSteps[step] = true;
                    pattern.Velocities[step, 2] = 60 + _rng.Next(20);
                }
                
                // Add occasional percussion hits
                if (_rng.NextDouble() < 0.15 && barStep % 5 == 0)
                {
                    pattern.PercussionSteps[step] = true;
                    pattern.Velocities[step, 5] = 70 + _rng.Next(20);
                }
                
                // Add occasional ambient toms
                if (_rng.NextDouble() < 0.15 && barStep % 7 == 0)
                {
                    pattern.TomSteps[step] = true;
                    pattern.Velocities[step, 4] = 60 + _rng.Next(30);
                }
            }
        }

        private static void AddCombatVariations(PercussionPattern pattern, int totalSteps, int stepsPerBar)
        {
            // Add intense combat rhythm variations
            
            // Double kick sections
            int startBar = _rng.Next(totalSteps / stepsPerBar - 1);
            int startStep = startBar * stepsPerBar;
            
            for (int i = 0; i < stepsPerBar / 2; i++)
            {
                int step = startStep + i * 2;
                pattern.KickSteps[step] = true;
                pattern.Velocities[step, 0] = 110;
            }
            
            // Intensify hi-hat pattern
            for (int step = 0; step < totalSteps; step++)
            {
                if (_rng.NextDouble() < 0.7) // 70% of steps have hi-hat
                {
                    pattern.HihatSteps[step] = true;
                    pattern.Velocities[step, 2] = 70 + (step % 4 == 0 ? 30 : 0); // Accent on quarter notes
                }
            }
            
            // Add tom rolls
            if (_complexity > 0.5)
            {
                int rollStart = _rng.Next(totalSteps - stepsPerBar);
                for (int i = 0; i < stepsPerBar / 2; i++)
                {
                    int step = rollStart + i;
                    pattern.TomSteps[step] = _rng.NextDouble() < 0.7;
                    pattern.Velocities[step, 4] = 90 + _rng.Next(30);
                }
            }
        }

        private static void AddPercussionFill(PercussionPattern pattern, int totalSteps, int stepsPerBar)
        {
            // Create a fill pattern for the last bar
            int fillStart = totalSteps - stepsPerBar;
            
            // Clear most of the existing pattern in the fill section
            for (int step = fillStart; step < totalSteps; step++)
            {
                // Keep kicks on 1, remove most others
                if (step > fillStart)
                {
                    pattern.KickSteps[step] = false;
                }
                pattern.SnareSteps[step] = false;
                pattern.HihatSteps[step] = false;
            }
            
            // Fill style depends on the track style
            if (_selectedStyle == 2) // Combat - intense tom/snare fills
            {
                // Fast tom rolls
                for (int i = 0; i < stepsPerBar - 1; i++)
                {
                    int step = fillStart + i;
                    
                    if (i % 3 == 0) // Distributed pattern
                    {
                        pattern.TomSteps[step] = true;
                        pattern.Velocities[step, 4] = 100 - (i * 3);
                    }
                    else if (i % 2 == 0)
                    {
                        pattern.SnareSteps[step] = true;
                        pattern.Velocities[step, 1] = 90 + (i * 2);
                    }
                }
                
                // Crash at the end
                pattern.CrashSteps[totalSteps - 1] = true;
                pattern.Velocities[totalSteps - 1, 3] = 120;
            }
            else if (_selectedStyle == 1) // Ambient - subtle fill
            {
                // Sparse, atmospheric fill
                for (int i = 0; i < stepsPerBar; i += 2)
                {
                    int step = fillStart + i;
                    
                    if (_rng.NextDouble() < 0.6)
                    {
                        pattern.TomSteps[step] = true;
                        pattern.Velocities[step, 4] = 80 + _rng.Next(20);
                    }
                }
                
                // Subtle ride cymbal swells
                for (int i = 0; i < stepsPerBar; i += 3)
                {
                    int step = fillStart + i;
                    pattern.HihatSteps[step] = true;
                    pattern.Velocities[step, 2] = 70 + _rng.Next(30);
                }
            }
            else // Industrial/default - glitchy, syncopated fill
            {
                // Snare build
                for (int i = 0; i < stepsPerBar; i++)
                {
                    int step = fillStart + i;
                    
                    if (i >= stepsPerBar / 2 && i % 2 == 0)
                    {
                        pattern.SnareSteps[step] = true;
                        pattern.Velocities[step, 1] = 90 + (i * 2);
                    }
                    else if (i >= stepsPerBar * 3 / 4 && i % 2 == 1)
                    {
                        // Double-time at the end
                        pattern.SnareSteps[step] = true;
                        pattern.Velocities[step, 1] = 100;
                    }
                }
                
                // Add industrial percussion
                for (int i = 0; i < stepsPerBar; i += 2)
                {
                    int step = fillStart + i;
                    if (_rng.NextDouble() < 0.4)
                    {
                        pattern.PercussionSteps[step] = true;
                        pattern.Velocities[step, 5] = 90 + _rng.Next(30);
                    }
                }
                
                // Crash at the end
                pattern.CrashSteps[totalSteps - 1] = true;
                pattern.Velocities[totalSteps - 1, 3] = 120;
            }
        }

        private static PercussionPattern ClonePercussionPattern(PercussionPattern original)
        {
            PercussionPattern clone = new PercussionPattern
            {
                KickSteps = (bool[])original.KickSteps.Clone(),
                SnareSteps = (bool[])original.SnareSteps.Clone(),
                HihatSteps = (bool[])original.HihatSteps.Clone(),
                CrashSteps = (bool[])original.CrashSteps.Clone(),
                TomSteps = (bool[])original.TomSteps.Clone(),
                PercussionSteps = (bool[])original.PercussionSteps.Clone(),
                Velocities = (int[,])original.Velocities.Clone()
            };
            return clone;
        }

        private static TrackPart GenerateArpeggio(MusicTrack track)
        {
            TrackPart arpeggio = new TrackPart
            {
                Type = TrackPartType.Arpeggio,
                Channel = 1,
                Program = 81 // Lead 1 (square)
            };
            
            // Create arpeggio patterns
            int patternLength = 2; // bars
            int stepsPerBar = 16;  // 16th notes
            int totalSteps = patternLength * stepsPerBar;
            
            // Create different arpeggio patterns
            List<ArpeggioPattern> patterns = new List<ArpeggioPattern>();
            
            // Base arpeggio pattern
            ArpeggioPattern basePattern = GenerateBaseArpeggio(track, totalSteps, stepsPerBar);
            patterns.Add(basePattern);
            
            // Create variations
            int numVariations = (int)(_complexity * 3) + 1;
            for (int v = 0; v < numVariations; v++)
            {
                ArpeggioPattern variation = CloneArpeggioPattern(basePattern);
                
                // Apply variations based on complexity
                for (int step = 0; step < totalSteps; step++)
                {
                    // Occasionally change the note
                    if (variation.Notes[step] != -1 && _rng.NextDouble() < 0.15)
                    {
                        // Choose a different note from the chord
                        int chordIndex = (step / (stepsPerBar * track.BarsPerChord)) % track.ChordProgression.Count;
                        int[] chord = track.ChordProgression[chordIndex];
                        
                        // Occasionally use extended chord tones
                        if (_complexity > 0.6 && _rng.NextDouble() < 0.3)
                        {
                            // Add 7th or 9th
                            int[] extendedChord = new int[chord.Length + 1];
                            Array.Copy(chord, extendedChord, chord.Length);
                            extendedChord[chord.Length] = (chord[0] + (_rng.NextDouble() < 0.5 ? 10 : 14)) % 12;
                            chord = extendedChord;
                        }
                        
                        // Choose a random note from the chord
                        int noteIndex = _rng.Next(chord.Length);
                        int note = chord[noteIndex];
                        
                        // Octave selection (C4 = 60)
                        int octave = 5; // C5 = 72
                        if (_rng.NextDouble() < 0.3) octave = 4; // Add some lower notes
                        
                        variation.Notes[step] = 60 + note + (octave - 4) * 12;
                    }
                    
                    // Occasionally remove notes for rhythmic variation
                    if (variation.Notes[step] != -1 && _rng.NextDouble() < 0.1)
                    {
                        variation.Notes[step] = -1; // Remove note
                    }
                    
                    // Vary velocities
                    if (variation.Notes[step] != -1)
                    {
                        variation.Velocities[step] = Math.Clamp(
                            variation.Velocities[step] + _rng.Next(-15, 16), 60, 127);
                    }
                }
                
                patterns.Add(variation);
            }
            
            ArpeggioPattern glitchPattern = GenerateGlitchArpeggio(track, totalSteps, stepsPerBar);
            patterns.Add(glitchPattern);
            
            // Assign patterns
            arpeggio.Patterns = patterns.Cast<Pattern>().ToList();
            
            // Generate notes
            GenerateNotesFromArpeggioPatterns(arpeggio, patterns, track.BarCount, stepsPerBar);
            
            return arpeggio;
        }

        private static ArpeggioPattern GenerateBaseArpeggio(MusicTrack track, int totalSteps, int stepsPerBar)
        {
            ArpeggioPattern pattern = new ArpeggioPattern
            {
                Notes = new int[totalSteps],
                Velocities = new int[totalSteps],
                Durations = new int[totalSteps]
            };
            
            // Initialize all to -1 (no note)
            for (int i = 0; i < totalSteps; i++)
            {
                pattern.Notes[i] = -1;
                pattern.Velocities[i] = 0;
                pattern.Durations[i] = 0;
            }
            
            // Arpeggio speed depends on intensity and style
            int arpDivision;
            if (_intensity < 0.4)
            {
                arpDivision = 4; // Quarter notes
            }
            else if (_intensity < 0.7)
            {
                arpDivision = 2; // 8th notes
            }
            else
            {
                arpDivision = 1; // 16th notes
            }
            
            // Arpeggio style (0 = up, 1 = down, 2 = up-down, 3 = random)
            int arpStyle = _rng.Next(4);
            
            // For each chord in the progression
            for (int bar = 0; bar < totalSteps / stepsPerBar; bar++)
            {
                int chordIndex = (bar / track.BarsPerChord) % track.ChordProgression.Count;
                int[] chord = track.ChordProgression[chordIndex];
                
                // Base octave (C4 = 60)
                int baseOctave = 5; // C5 = 72
                
                // Create note array from chord tones across octaves
                List<int> arpNotes = new List<int>();
                
                // Add chord tones
                foreach (int note in chord)
                {
                    arpNotes.Add(60 + note + (baseOctave - 4) * 12);
                }
                
                // Add higher octave for up-down patterns
                if (arpStyle == 2)
                {
                    foreach (int note in chord)
                    {
                        arpNotes.Add(60 + note + (baseOctave - 3) * 12);
                    }
                }
                
                // Apply the arpeggio pattern
                for (int step = 0; step < stepsPerBar; step += arpDivision)
                {
                    int currentStep = bar * stepsPerBar + step;
                    if (currentStep >= totalSteps) break;
                    
                    int noteIndex;
                    
                    switch (arpStyle)
                    {
                        case 0: // Up
                            noteIndex = (step / arpDivision) % arpNotes.Count;
                            break;
                        case 1: // Down
                            noteIndex = arpNotes.Count - 1 - ((step / arpDivision) % arpNotes.Count);
                            break;
                        case 2: // Up-Down
                            int cycleLength = arpNotes.Count * 2 - 2;
                            int position = (step / arpDivision) % cycleLength;
                            noteIndex = position < arpNotes.Count 
                                ? position 
                                : cycleLength - position;
                            break;
                        default: // Random
                            noteIndex = _rng.Next(arpNotes.Count);
                            break;
                    }
                    
                    pattern.Notes[currentStep] = arpNotes[noteIndex];
                    pattern.Velocities[currentStep] = 80 + (_rng.Next(40) * (step % 8 == 0 ? 1 : 0)); // Accent on beats
                    pattern.Durations[currentStep] = Math.Max(1, arpDivision - 1); // Slightly detached
                }
            }
            
            return pattern;
        }

        private static ArpeggioPattern GenerateGlitchArpeggio(MusicTrack track, int totalSteps, int stepsPerBar)
        {
            ArpeggioPattern pattern = new ArpeggioPattern
            {
                Notes = new int[totalSteps],
                Velocities = new int[totalSteps],
                Durations = new int[totalSteps]
            };
            
            // Initialize all to -1 (no note)
            for (int i = 0; i < totalSteps; i++)
            {
                pattern.Notes[i] = -1;
                pattern.Velocities[i] = 0;
                pattern.Durations[i] = 0;
            }
            
            // Get first chord for reference
            int[] chord = track.ChordProgression[0];
            
            // Create a glitchy, unpredictable pattern
            for (int step = 0; step < totalSteps; step++)
            {
                // Random distribution of notes - more dense in certain areas
                if (_rng.NextDouble() < (0.3 + (_intensity * 0.4)))
                {
                    // Choose a note from the chord
                    int noteIndex = _rng.Next(chord.Length);
                    int note = chord[noteIndex];
                    
                    // Random octave selection (typically higher for glitch arps)
                    int octave = 5 + _rng.Next(2); // C5 or C6
                    
                    pattern.Notes[step] = 60 + note + (octave - 4) * 12;
                    pattern.Velocities[step] = 70 + _rng.Next(50);
                    pattern.Durations[step] = 1; // Very short, staccato notes
                }
                
                // Add occasional pitch bend artifacts
                if (_rng.NextDouble() < 0.1 && step > 0 && pattern.Notes[step - 1] != -1)
                {
                    pattern.Notes[step] = pattern.Notes[step - 1] + (_rng.Next(3) - 1);
                    pattern.Velocities[step] = pattern.Velocities[step - 1] - 20;
                    pattern.Durations[step] = 1;
                }
            }
            
            // Create some repeating "stuck note" patterns
            if (_complexity > 0.6)
            {
                int startStep = _rng.Next(totalSteps / 2);
                int endStep = Math.Min(startStep + 8 + _rng.Next(8), totalSteps);
                int repeatingNote = 60 + chord[_rng.Next(chord.Length)] + 12;
                
                for (int step = startStep; step < endStep; step += 2)
                {
                    pattern.Notes[step] = repeatingNote;
                    pattern.Velocities[step] = 100;
                    pattern.Durations[step] = 1;
                }
            }
            
            return pattern;
        }

        private static ArpeggioPattern CloneArpeggioPattern(ArpeggioPattern original)
        {
            ArpeggioPattern clone = new ArpeggioPattern
            {
                Notes = (int[])original.Notes.Clone(),
                Velocities = (int[])original.Velocities.Clone(),
                Durations = (int[])original.Durations.Clone()
            };
            return clone;
        }

        private static TrackPart GeneratePads(MusicTrack track)
        {
            TrackPart pads = new TrackPart
            {
                Type = TrackPartType.Pad,
                Channel = 2,
                Program = 91 // Pad 4 (choir)
            };
            
            // Pads are longer sustained chords
            int patternLength = track.BarsPerChord * 2; // One pattern per 2 chord changes
            int stepsPerBar = 16;  // 16th notes
            int totalSteps = patternLength * stepsPerBar;
            
            // Create pad patterns
            List<PadPattern> patterns = new List<PadPattern>();
            
            // Main pad pattern
            PadPattern mainPattern = new PadPattern
            {
                Notes = new List<int[]>(),
                StartSteps = new List<int>(),
                Durations = new List<int>(),
                Velocities = new List<int>()
            };
            
            // Create sustained chord pads
            for (int bar = 0; bar < patternLength; bar += track.BarsPerChord)
            {
                int chordIndex = (bar / track.BarsPerChord) % track.ChordProgression.Count;
                int[] chord = track.ChordProgression[chordIndex];
                
                // Start step for this chord
                int startStep = bar * stepsPerBar;
                
                // Adjust chord voicing for pads - more open voicing
                List<int> padNotes = new List<int>();
                
                // Base octave (C3 = 48)
                int baseOctave = 4; // C4 = 60
                
                // Add chord notes with pad-friendly voicing
                for (int i = 0; i < chord.Length; i++)
                {
                    int note = chord[i];
                    // Spread notes across octaves
                    int octaveAdjust = 0;
                    if (i == 0) octaveAdjust = -1; // Root note lower
                    
                    padNotes.Add(60 + note + (octaveAdjust + baseOctave - 4) * 12);
                }
                
                // Sometimes add 7th or 9th for richer pads
                if (_complexity > 0.5 && _rng.NextDouble() < 0.6)
                {
                    // Add 7th
                    int seventh = track.IsMinor ? 10 : 11; // Minor 7th or Major 7th
                    padNotes.Add(60 + ((chord[0] + seventh) % 12) + (baseOctave - 4) * 12);
                }
                
                if (_complexity > 0.7 && _rng.NextDouble() < 0.4)
                {
                    // Add 9th
                    int ninth = 14; // 9th scale degree
                    padNotes.Add(60 + ((chord[0] + ninth) % 12) + (baseOctave - 4) * 12);
                }
                
                // Add to pattern
                mainPattern.Notes.Add(padNotes.ToArray());
                mainPattern.StartSteps.Add(startStep);
                
                // Duration - slightly shorter than chord length for breathing room
                int duration = track.BarsPerChord * stepsPerBar - 4;
                mainPattern.Durations.Add(duration);
                
                // Velocity - pads are typically softer
                mainPattern.Velocities.Add(70 + _rng.Next(20));
            }
            
            patterns.Add(mainPattern);
            
            // Create variation with different voicings
            PadPattern variation = new PadPattern
            {
                Notes = new List<int[]>(),
                StartSteps = new List<int>(),
                Durations = new List<int>(),
                Velocities = new List<int>()
            };
            
            // Create sustained chord pads with different voicings
            for (int bar = 0; bar < patternLength; bar += track.BarsPerChord)
            {
                int chordIndex = (bar / track.BarsPerChord) % track.ChordProgression.Count;
                int[] chord = track.ChordProgression[chordIndex];
                
                // Start step for this chord
                int startStep = bar * stepsPerBar;
                
                // Higher and sparser voicing for variation
                List<int> padNotes = new List<int>();
                
                // Base octave (C4 = 60) - higher for variation
                int baseOctave = 5; // C5 = 72
                
                // Add selected chord tones
                padNotes.Add(60 + chord[0] + (baseOctave - 4) * 12); // Root
                
                // Maybe skip the third for more ambiguous sound
                if (_rng.NextDouble() < 0.7)
                {
                    padNotes.Add(60 + chord[1] + (baseOctave - 4) * 12); // Third
                }
                
                padNotes.Add(60 + chord[2] + (baseOctave - 4) * 12); // Fifth
                
                // Often add 7th for tension
                if (_rng.NextDouble() < 0.8)
                {
                    int seventh = track.IsMinor ? 10 : 11; // Minor 7th or Major 7th
                    padNotes.Add(60 + ((chord[0] + seventh) % 12) + (baseOctave - 4) * 12);
                }
                
                // Add to pattern
                variation.Notes.Add(padNotes.ToArray());
                variation.StartSteps.Add(startStep);
                
                // Duration - shorter for this variation to create space
                int duration = track.BarsPerChord * stepsPerBar - 8;
                variation.Durations.Add(duration);
                
                // Velocity - even softer
                variation.Velocities.Add(60 + _rng.Next(20));
            }
            
            patterns.Add(variation);
            
            PadPattern tensionPad = new PadPattern
            {
                Notes = new List<int[]>(),
                StartSteps = new List<int>(),
                Durations = new List<int>(),
                Velocities = new List<int>()
            };
            
            // Create dissonant tension pads
            for (int bar = 0; bar < patternLength; bar += track.BarsPerChord)
            {
                int chordIndex = (bar / track.BarsPerChord) % track.ChordProgression.Count;
                int[] chord = track.ChordProgression[chordIndex];
                
                // Start step for this chord - offset for tension effect
                int startStep = bar * stepsPerBar + 4;
                
                // Create dissonant voicing
                List<int> tensionNotes = new List<int>();
                
                // Base octave
                int baseOctave = 4; // C4 = 60
                
                // Add root and fifth
                tensionNotes.Add(60 + chord[0] + (baseOctave - 4) * 12);
                tensionNotes.Add(60 + chord[2] + (baseOctave - 4) * 12);
                
                // Add dissonant tones
                tensionNotes.Add(60 + ((chord[0] + 1) % 12) + (baseOctave - 4) * 12); // b9 or b2
                
                // Add to pattern
                tensionPad.Notes.Add(tensionNotes.ToArray());
                tensionPad.StartSteps.Add(startStep);
                
                // Duration - shorter for tension
                int duration = (track.BarsPerChord * stepsPerBar) / 2;
                tensionPad.Durations.Add(duration);
                
                // Velocity - softer for background tension
                tensionPad.Velocities.Add(50 + _rng.Next(20));
            }
            
            patterns.Add(tensionPad);
            
            // Assign patterns
            pads.Patterns = patterns.Cast<Pattern>().ToList();
            
            // Generate notes
            GenerateNotesFromPadPatterns(pads, patterns, track.BarCount, stepsPerBar);
            
            return pads;
        }

        private static TrackPart GenerateLead(MusicTrack track)
        {
            TrackPart lead = new TrackPart
            {
                Type = TrackPartType.Lead,
                Channel = 3,
                Program = 80 // Lead 1 (square)
            };
            
            // Lead patterns are typically 4 bars
            int patternLength = 4; // bars
            int stepsPerBar = 16;  // 16th notes
            int totalSteps = patternLength * stepsPerBar;
            
            // Create lead patterns
            List<LeadPattern> patterns = new List<LeadPattern>();
            
            // Main lead pattern
            LeadPattern mainPattern = GenerateMainLeadPattern(track, totalSteps, stepsPerBar);
            patterns.Add(mainPattern);
            
            // Create variations
            int numVariations = (int)(_complexity * 2) + 1;
            for (int v = 0; v < numVariations; v++)
            {
                LeadPattern variation = CloneLeadPattern(mainPattern);
                
                // Apply variations
                for (int step = 0; step < totalSteps; step++)
                {
                    // Skip steps without notes
                    if (variation.Notes[step] == -1) continue;
                    
                    // Sometimes change the pitch
                    if (_rng.NextDouble() < 0.15)
                    {
                        // Get current chord
                        int bar = step / stepsPerBar;
                        int chordIndex = (bar / track.BarsPerChord) % track.ChordProgression.Count;
                        int[] chord = track.ChordProgression[chordIndex];
                        
                        // Select scale degree
                        int[] scale = GetScale(track);
                        int randomDegree = _rng.Next(scale.Length);
                        int newNote = 60 + ((track.Key + scale[randomDegree]) % 12) + ((variation.Notes[step] / 12) - 5) * 12;
                        
                        variation.Notes[step] = newNote;
                    }
                    
                    // Sometimes remove notes
                    if (_rng.NextDouble() < 0.1)
                    {
                        variation.Notes[step] = -1;
                    }
                    
                    // Vary velocities
                    if (variation.Notes[step] != -1)
                    {
                        variation.Velocities[step] = Math.Clamp(
                            variation.Velocities[step] + _rng.Next(-10, 11), 60, 127);
                    }
                    
                    // Vary durations
                    if (variation.Notes[step] != -1)
                    {
                        variation.Durations[step] = Math.Max(1, 
                            variation.Durations[step] + _rng.Next(-1, 2));
                    }
                }
                
                patterns.Add(variation);
            }
            
            // Add a sparse motif pattern that's more memorable
            LeadPattern motifPattern = GenerateMotifLeadPattern(track, totalSteps, stepsPerBar);
            patterns.Add(motifPattern);
            
            // Assign patterns
            lead.Patterns = patterns.Cast<Pattern>().ToList();
            
            // Generate notes
            GenerateNotesFromLeadPatterns(lead, patterns, track.BarCount, stepsPerBar);
            
            return lead;
        }

        private static LeadPattern GenerateMainLeadPattern(MusicTrack track, int totalSteps, int stepsPerBar)
        {
            LeadPattern pattern = new LeadPattern
            {
                Notes = new int[totalSteps],
                Velocities = new int[totalSteps],
                Durations = new int[totalSteps]
            };
            
            // Initialize all to -1 (no note)
            for (int i = 0; i < totalSteps; i++)
            {
                pattern.Notes[i] = -1;
                pattern.Velocities[i] = 0;
                pattern.Durations[i] = 0;
            }
            
            // Get scale
            int[] scale = GetScale(track);
            
            // Determine lead melody rhythm based on intensity
            double noteProbability;
            if (_intensity < 0.4)
            {
                noteProbability = 0.2; // Sparse melody
            }
            else if (_intensity < 0.7)
            {
                noteProbability = 0.3; // Medium density
            }
            else
            {
                noteProbability = 0.4; // More dense melody
            }
            
            // Generate lead melody
            for (int bar = 0; bar < totalSteps / stepsPerBar; bar++)
            {
                int chordIndex = (bar / track.BarsPerChord) % track.ChordProgression.Count;
                int[] chord = track.ChordProgression[chordIndex];
                
                for (int beat = 0; beat < 4; beat++)
                {
                    int beatStart = bar * stepsPerBar + beat * 4;
                    
                    // Higher probability for notes on the beat
                    if (_rng.NextDouble() < noteProbability * 1.5)
                    {
                        // Choose a note, favoring chord tones
                        int noteChoice = _rng.NextDouble() < 0.7 ? 
                            // Chord tone
                            chord[_rng.Next(chord.Length)] :
                            // Scale tone
                            scale[_rng.Next(scale.Length)];
                        
                        // Adjust for actual key
                        int note = (track.Key + noteChoice) % 12;
                        
                        // Octave selection (C4 = 60, C5 = 72)
                        int octave = 5; // Lead typically in C5 range
                        
                        pattern.Notes[beatStart] = 60 + note + (octave - 4) * 12;
                        pattern.Velocities[beatStart] = 90 + (beat == 0 ? 20 : 0); // Accent on downbeat
                        pattern.Durations[beatStart] = 3; // Slightly detached
                    }
                    
                    // Off-beat notes (on 8th notes)
                    for (int i = 1; i < 4; i += 2)
                    {
                        int step = beatStart + i;
                        if (_rng.NextDouble() < noteProbability * 0.8)
                        {
                            // More likely to use scale tones for passing notes
                            int noteChoice = _rng.NextDouble() < 0.4 ? 
                                chord[_rng.Next(chord.Length)] :
                                scale[_rng.Next(scale.Length)];
                            
                            // Adjust for actual key
                            int note = (track.Key + noteChoice) % 12;
                            
                            // Octave selection
                            int octave = 5;
                            if (_rng.NextDouble() < 0.2) octave = 6; // Occasional high notes
                            
                            pattern.Notes[step] = 60 + note + (octave - 4) * 12;
                            pattern.Velocities[step] = 80 + _rng.Next(20);
                            pattern.Durations[step] = 2;
                        }
                    }
                    
                    // 16th note runs occasionally
                    if (beat % 2 == 1 && _complexity > 0.6 && _rng.NextDouble() < 0.3)
                    {
                        // Create a short run
                        for (int i = 0; i < 4; i++)
                        {
                            int step = beatStart + i;
                            if (_rng.NextDouble() < 0.7) // Not every 16th
                            {
                                // Use scale tones for runs
                                int scaleDegree = _rng.Next(scale.Length);
                                int note = (track.Key + scale[scaleDegree]) % 12;
                                
                                int octave = 5;
                                pattern.Notes[step] = 60 + note + (octave - 4) * 12;
                                pattern.Velocities[step] = 75 + _rng.Next(20);
                                pattern.Durations[step] = 1; // Very short for runs
                            }
                        }
                    }
                }
            }
            
            // Create melodic contour and phrasing
            ApplyMelodicPhrasing(pattern, totalSteps, stepsPerBar);
            
            return pattern;
        }

        private static LeadPattern GenerateMotifLeadPattern(MusicTrack track, int totalSteps, int stepsPerBar)
        {
            LeadPattern pattern = new LeadPattern
            {
                Notes = new int[totalSteps],
                Velocities = new int[totalSteps],
                Durations = new int[totalSteps]
            };
            
            // Initialize all to -1 (no note)
            for (int i = 0; i < totalSteps; i++)
            {
                pattern.Notes[i] = -1;
                pattern.Velocities[i] = 0;
                pattern.Durations[i] = 0;
            }
            
            // Get scale
            int[] scale = GetScale(track);
            
            // Create a short, memorable motif
            int motifLength = 4 + _rng.Next(5); // 4-8 notes
            int[] motif = new int[motifLength];
            int[] motifRhythm = new int[motifLength]; // Steps between notes
            
            // Generate the motif
            for (int i = 0; i < motifLength; i++)
            {
                // Choose note from scale, favoring chord tones for stability
                int chordIndex = 0; // Use first chord for motif
                int[] chord = track.ChordProgression[chordIndex];
                
                int scaleDegree;
                if (i == 0 || i == motifLength - 1)
                {
                    // Start and end on chord tones
                    int chordTone = _rng.Next(chord.Length);
                    scaleDegree = Array.IndexOf(scale, chord[chordTone] % 12);
                    if (scaleDegree == -1) scaleDegree = 0; // Fallback
                }
                else
                {
                    // Any scale tone for middle notes
                    scaleDegree = _rng.Next(scale.Length);
                }
                
                motif[i] = scaleDegree;
                
                // Rhythm - mix of longer and shorter notes
                if (i == 0)
                {
                    motifRhythm[i] = 0; // First note starts at beginning
                }
                else
                {
                    // Distance from previous note
                    motifRhythm[i] = 2 + _rng.Next(4); // 2-5 steps
                }
            }
            
            // Place motif at strategic positions
            int[] motifPositions = { 0, stepsPerBar * 2 }; // Bar 1 and bar 3
            
            foreach (int startPos in motifPositions)
            {
                int pos = startPos;
                for (int i = 0; i < motifLength; i++)
                {
                    if (pos >= totalSteps) break;
                    
                    // Convert scale degree to actual note
                    int note = (track.Key + scale[motif[i]]) % 12;
                    
                    // Octave selection (C5 = 72)
                    int octave = 5;
                    
                    pattern.Notes[pos] = 60 + note + (octave - 4) * 12;
                    pattern.Velocities[pos] = 100 - (i * 3); // Fade out slightly
                    pattern.Durations[pos] = 3; // Slightly detached
                    
                    // Move to next note position
                    if (i < motifLength - 1)
                    {
                        pos += motifRhythm[i + 1];
                    }
                }
            }
            
            return pattern;
        }

        private static void ApplyMelodicPhrasing(LeadPattern pattern, int totalSteps, int stepsPerBar)
        {
            // Apply melodic shaping to create more musical phrases
            
            // Find phrase boundaries (typically every 8 beats)
            int phraseLength = stepsPerBar / 2; // 8 beats
            
            for (int phraseStart = 0; phraseStart < totalSteps; phraseStart += phraseLength)
            {
                int phraseEnd = Math.Min(phraseStart + phraseLength, totalSteps);
                
                // Find all notes in the phrase
                List<int> noteIndices = new List<int>();
                for (int step = phraseStart; step < phraseEnd; step++)
                {
                    if (pattern.Notes[step] != -1)
                    {
                        noteIndices.Add(step);
                    }
                }
                
                if (noteIndices.Count < 2) continue; // Need at least two notes for phrasing
                
                // Adjust velocities for phrasing
                for (int i = 0; i < noteIndices.Count; i++)
                {
                    int step = noteIndices[i];
                    
                    // Shape depends on position in phrase
                    double position = (double)i / noteIndices.Count;
                    
                    // Arch-shaped velocity curve (start strong, dip in middle, end strong)
                    double velocityFactor = 0.8 + (0.2 * Math.Sin(position * Math.PI));
                    
                    // Apply velocity adjustment
                    pattern.Velocities[step] = (int)(pattern.Velocities[step] * velocityFactor);
                    
                    // Adjust duration - longer at phrase boundaries
                    if (i == 0 || i == noteIndices.Count - 1)
                    {
                        pattern.Durations[step] = Math.Min(4, pattern.Durations[step] + 1);
                    }
                }
            }
        }

        private static LeadPattern CloneLeadPattern(LeadPattern original)
        {
            LeadPattern clone = new LeadPattern
            {
                Notes = (int[])original.Notes.Clone(),
                Velocities = (int[])original.Velocities.Clone(),
                Durations = (int[])original.Durations.Clone()
            };
            return clone;
        }

        private static int[] GetScale(MusicTrack track)
        {
            // Return scale degrees based on key type
            if (track.IsMinor)
            {
                // Natural minor scale
                return new int[] { 0, 2, 3, 5, 7, 8, 10 };
            }
            else
            {
                // Major scale
                return new int[] { 0, 2, 4, 5, 7, 9, 11 };
            }
        }

        private static void AddGlitchEffects(MusicTrack track)
        {
            // Only add glitch effects if enabled
            if (!_includeGlitch) return;
            
            // Add a glitch effects part
            TrackPart glitchPart = new TrackPart
            {
                Type = TrackPartType.GlitchFx,
                Channel = 4,
                Program = 86 // Lead 7 (fifths)
            };
            
            // Create glitch patterns
            int patternLength = 2; // bars
            int stepsPerBar = 16;  // 16th notes
            int totalSteps = patternLength * stepsPerBar;
            
            // Create different glitch patterns
            List<GlitchPattern> patterns = new List<GlitchPattern>();
            
            // Main glitch pattern
            GlitchPattern mainPattern = new GlitchPattern
            {
                Notes = new int[totalSteps],
                Velocities = new int[totalSteps],
                Durations = new int[totalSteps],
                Effects = new GlitchEffect[totalSteps]
            };
            
            // Initialize all to -1 (no note)
            for (int i = 0; i < totalSteps; i++)
            {
                mainPattern.Notes[i] = -1;
                mainPattern.Velocities[i] = 0;
                mainPattern.Durations[i] = 0;
                mainPattern.Effects[i] = GlitchEffect.None;
            }
            
            // Add glitch effects based on complexity and style
            if (_selectedStyle == 2) // Combat - aggressive glitches
            {
                GenerateCombatGlitches(mainPattern, totalSteps, stepsPerBar);
            }
            else if (_selectedStyle == 1) // Ambient - subtle glitches
            {
                GenerateAmbientGlitches(mainPattern, totalSteps, stepsPerBar);
            }
            else
            {
                GenerateIndustrialGlitches(mainPattern, totalSteps, stepsPerBar);
            }
            
            patterns.Add(mainPattern);
            
            // Generate more variations
            int numVariations = (int)(_complexity * 2) + 1;
            for (int v = 0; v < numVariations; v++)
            {
                GlitchPattern variation = CloneGlitchPattern(mainPattern);
                
                // Modify the variation - randomize more
                for (int i = 0; i < totalSteps; i++)
                {
                    // Randomize effects
                    if (_rng.NextDouble() < 0.2)
                    {
                        variation.Effects[i] = (GlitchEffect)_rng.Next(5);
                    }
                    
                    // Randomize notes
                    if (variation.Notes[i] != -1 && _rng.NextDouble() < 0.3)
                    {
                        // Get random note from key scale
                        int[] scale = GetScale(track);
                        int note = track.Key + scale[_rng.Next(scale.Length)];
                        
                        // Random octave (typically high for glitches)
                        int octave = 5 + _rng.Next(2);
                        
                        variation.Notes[i] = 60 + note + (octave - 4) * 12;
                        variation.Velocities[i] = 70 + _rng.Next(50);
                    }
                }
                
                patterns.Add(variation);
            }
            
            // Add a drop pattern (silence followed by intense effects)
            GlitchPattern dropPattern = GenerateDropPattern(track, totalSteps, stepsPerBar);
            patterns.Add(dropPattern);
            
            // Assign patterns
            glitchPart.Patterns = patterns.Cast<Pattern>().ToList();
            
            // Generate notes
            GenerateNotesFromGlitchPatterns(glitchPart, patterns, track.BarCount, stepsPerBar);
            
            // Add to track
            track.Parts.Add(glitchPart);
        }

        private static void GenerateIndustrialGlitches(GlitchPattern pattern, int totalSteps, int stepsPerBar)
        {
            // Add rhythmic stutter effect
            int stutterStart = _rng.Next(totalSteps - 8);
            for (int i = 0; i < 4; i++)
            {
                int step = stutterStart + i * 2;
                if (step < totalSteps)
                {
                    pattern.Notes[step] = 72 + _rng.Next(12); // High notes
                    pattern.Velocities[step] = 100;
                    pattern.Durations[step] = 1; // Very short
                    pattern.Effects[step] = GlitchEffect.Stutter;
                }
            }
            
            // Add digital noise bursts
            for (int bar = 0; bar < totalSteps / stepsPerBar; bar++)
            {
                if (_rng.NextDouble() < 0.7) // 70% chance per bar
                {
                    int step = bar * stepsPerBar + _rng.Next(stepsPerBar);
                    pattern.Notes[step] = 84 + _rng.Next(12); // Very high notes
                    pattern.Velocities[step] = 90 + _rng.Next(30);
                    pattern.Durations[step] = 2;
                    pattern.Effects[step] = GlitchEffect.DigitalNoise;
                }
            }
            
            // Add pitch bend artifacts
            int bendStart = _rng.Next(totalSteps - 4);
            for (int i = 0; i < 4; i++)
            {
                int step = bendStart + i;
                if (step < totalSteps)
                {
                    pattern.Notes[step] = 60 + i * 2; // Rising pitch
                    pattern.Velocities[step] = 80 - i * 10; // Fading out
                    pattern.Durations[step] = 1;
                    pattern.Effects[step] = GlitchEffect.PitchBend;
                }
            }
            
            // Add filter sweep effect
            int sweepStart = _rng.Next(totalSteps - stepsPerBar);
            for (int i = 0; i < 8; i++)
            {
                int step = sweepStart + i;
                if (step < totalSteps)
                {
                    pattern.Notes[step] = 72; // C5
                    pattern.Velocities[step] = 70 + i * 5; // Rising volume
                    pattern.Durations[step] = 1;
                    pattern.Effects[step] = GlitchEffect.FilterSweep;
                }
            }
        }

        private static void GenerateCombatGlitches(GlitchPattern pattern, int totalSteps, int stepsPerBar)
        {
            // Combat glitches - more aggressive and intense
            
            // Add aggressive stutter
            for (int bar = 0; bar < totalSteps / stepsPerBar; bar++)
            {
                if (_rng.NextDouble() < 0.8) // 80% chance per bar
                {
                    int beatStart = bar * stepsPerBar + _rng.Next(4) * 4; // Random beat in bar
                    
                    for (int i = 0; i < 4; i++)
                    {
                        int step = beatStart + i;
                        if (step < totalSteps)
                        {
                            pattern.Notes[step] = 80 - i; // Descending notes
                            pattern.Velocities[step] = 110 - i * 5;
                            pattern.Durations[step] = 1;
                            pattern.Effects[step] = (i % 2 == 0) ? 
                                GlitchEffect.Stutter : GlitchEffect.DigitalNoise;
                        }
                    }
                }
            }
            
            // Add impact hits
            for (int bar = 0; bar < totalSteps / stepsPerBar; bar++)
            {
                if (_rng.NextDouble() < 0.5) // 50% chance per bar
                {
                    int step = bar * stepsPerBar; // On the downbeat
                    pattern.Notes[step] = 48; // Low note for impact
                    pattern.Velocities[step] = 127; // Maximum velocity
                    pattern.Durations[step] = 3;
                    pattern.Effects[step] = GlitchEffect.Distortion;
                }
            }
            
            // Add rapid filter sweeps
            int sweepStart = _rng.Next(totalSteps - 8);
            for (int i = 0; i < 8; i++)
            {
                int step = sweepStart + i;
                if (step < totalSteps)
                {
                    pattern.Notes[step] = 60 + (i % 2) * 12; // Alternating octaves
                    pattern.Velocities[step] = 90;
                    pattern.Durations[step] = 1;
                    pattern.Effects[step] = GlitchEffect.FilterSweep;
                }
            }
            
            // Add aggressive risers
            int riserStart = totalSteps - stepsPerBar; // Last bar
            for (int i = 0; i < stepsPerBar; i++)
            {
                int step = riserStart + i;
                if (step < totalSteps && i % 2 == 0)
                {
                    pattern.Notes[step] = 60 + i; // Rising notes
                    pattern.Velocities[step] = 80 + (i * 40 / stepsPerBar); // Rising volume
                    pattern.Durations[step] = 1;
                    pattern.Effects[step] = GlitchEffect.RiserFall;
                }
            }
        }

        private static void GenerateAmbientGlitches(GlitchPattern pattern, int totalSteps, int stepsPerBar)
        {
            // Ambient glitches - more subtle and atmospheric
            
            // Add subtle texture changes
            for (int bar = 0; bar < totalSteps / stepsPerBar; bar++)
            {
                if (_rng.NextDouble() < 0.4) // 40% chance per bar
                {
                    int step = bar * stepsPerBar + _rng.Next(stepsPerBar);
                    pattern.Notes[step] = 72 + _rng.Next(12); // High notes
                    pattern.Velocities[step] = 60 + _rng.Next(20); // Softer
                    pattern.Durations[step] = 4 + _rng.Next(4); // Longer
                    pattern.Effects[step] = GlitchEffect.FilterSweep;
                }
            }
            
            // Add occasional digital artifacts
            for (int i = 0; i < 3; i++)
            {
                int step = _rng.Next(totalSteps);
                pattern.Notes[step] = 84 + _rng.Next(12);
                pattern.Velocities[step] = 50 + _rng.Next(30);
                pattern.Durations[step] = 2;
                pattern.Effects[step] = GlitchEffect.DigitalNoise;
            }
            
            // Add subtle pitch bends
            int bendStart = _rng.Next(totalSteps - 8);
            for (int i = 0; i < 8; i++)
            {
                int step = bendStart + i;
                if (step < totalSteps && i % 2 == 0)
                {
                    pattern.Notes[step] = 60 + i; // Slowly rising
                    pattern.Velocities[step] = 60;
                    pattern.Durations[step] = 3;
                    pattern.Effects[step] = GlitchEffect.PitchBend;
                }
            }
            
            // Add atmospheric swells
            int swellStart = _rng.Next(totalSteps - stepsPerBar);
            for (int i = 0; i < stepsPerBar; i += 4)
            {
                int step = swellStart + i;
                if (step < totalSteps)
                {
                    pattern.Notes[step] = 72;
                    pattern.Velocities[step] = 40 + (i * 20 / stepsPerBar);
                    pattern.Durations[step] = 8;
                    pattern.Effects[step] = GlitchEffect.FilterSweep;
                }
            }
        }

        private static GlitchPattern GenerateDropPattern(MusicTrack track, int totalSteps, int stepsPerBar)
        {
            // Create a "drop" pattern - silence followed by intense effects
            GlitchPattern pattern = new GlitchPattern
            {
                Notes = new int[totalSteps],
                Velocities = new int[totalSteps],
                Durations = new int[totalSteps],
                Effects = new GlitchEffect[totalSteps]
            };
            
            for (int i = 0; i < totalSteps; i++)
            {
                pattern.Notes[i] = -1;
                pattern.Velocities[i] = 0;
                pattern.Durations[i] = 0;
                pattern.Effects[i] = GlitchEffect.None;
            }
            
            // First half - almost total silence (the "drop")
            int dropLength = totalSteps / 2;
            
            // Just one hint of what's coming
            if (_complexity > 0.5)
            {
                int step = dropLength / 2;
                pattern.Notes[step] = 84;
                pattern.Velocities[step] = 60;
                pattern.Durations[step] = 1;
                pattern.Effects[step] = GlitchEffect.DigitalNoise;
            }
            
            // Second half - intense comeback
            for (int i = dropLength; i < totalSteps; i++)
            {
                // First beat after drop - major impact
                if (i == dropLength)
                {
                    pattern.Notes[i] = 48; // Low note
                    pattern.Velocities[i] = 127; // Full velocity
                    pattern.Durations[i] = 6; // Long note
                    pattern.Effects[i] = GlitchEffect.Distortion;
                    continue;
                }
                
                // Remainder of pattern - intense rhythmic effects
                if ((i - dropLength) % 2 == 0 && _rng.NextDouble() < 0.8)
                {
                    // Get a note from the first chord
                    int[] chord = track.ChordProgression[0];
                    int note = chord[_rng.Next(chord.Length)];
                    
                    // Map to actual key and high octave
                    int actualNote = 60 + ((track.Key + note) % 12) + 12; // C5 or higher
                    
                    pattern.Notes[i] = actualNote;
                    pattern.Velocities[i] = 100 + _rng.Next(28);
                    pattern.Durations[i] = 1;
                    
                    // Alternate between effect types
                    if ((i - dropLength) % 4 == 0)
                    {
                        pattern.Effects[i] = GlitchEffect.Stutter;
                    }
                    else if ((i - dropLength) % 4 == 2)
                    {
                        pattern.Effects[i] = GlitchEffect.DigitalNoise;
                    }
                }
            }
            
            return pattern;
        }

        private static GlitchPattern CloneGlitchPattern(GlitchPattern original)
        {
            GlitchPattern clone = new GlitchPattern
            {
                Notes = (int[])original.Notes.Clone(),
                Velocities = (int[])original.Velocities.Clone(),
                Durations = (int[])original.Durations.Clone(),
                Effects = (GlitchEffect[])original.Effects.Clone()
            };
            return clone;
        }

        private static void GenerateNotesFromPatterns(TrackPart part, int totalBars, int stepsPerBar)
        {
            // Generate notes from patterns for the full track length
            // This is mainly used for bass and similar patterns
            
            if (part.Patterns.Count == 0) return;
            
            // Clear existing notes
            part.Notes.Clear();
            
            // Get steps per pattern
            Pattern firstPattern = part.Patterns[0];
            int patternBars = 0;
            
            if (firstPattern is BassPattern bassPattern)
            {
                patternBars = bassPattern.Steps.Length / stepsPerBar;
            }
            else
            {
                // Default to 4 bars if pattern type is unknown
                patternBars = 4;
            }
            
            // Fill track with patterns
            for (int bar = 0; bar < totalBars; bar++)
            {
                // Select pattern based on position
                int patternIndex;
                
                // Use main pattern most of the time
                if (bar < totalBars / 4) // First quarter - main pattern
                {
                    patternIndex = 0;
                }
                else if (bar >= totalBars - 4) // Last 4 bars - final pattern (often a fill)
                {
                    patternIndex = part.Patterns.Count - 1;
                }
                else // Middle section - mix of patterns
                {
                    // More variations in the middle
                    double randomFactor = _complexity * 0.7;
                    
                    if (_rng.NextDouble() < randomFactor)
                    {
                        // Use a variation
                        patternIndex = 1 + _rng.Next(part.Patterns.Count - 2);
                    }
                    else
                    {
                        // Use main pattern
                        patternIndex = 0;
                    }
                }
                
                // Apply the selected pattern for this bar
                Pattern selectedPattern = part.Patterns[patternIndex];
                
                // Different handling based on pattern type
                if (selectedPattern is BassPattern bassPat)
                {
                    ApplyBassPatternToBar(part, bassPat, bar, patternBars, stepsPerBar);
                }
            }
        }

        private static void ApplyBassPatternToBar(TrackPart part, BassPattern pattern, int currentBar, int patternBars, int stepsPerBar)
        {
            // Apply a bass pattern to a specific bar
            int patternOffset = (currentBar % patternBars) * stepsPerBar;
            
            for (int step = 0; step < stepsPerBar; step++)
            {
                int patternStep = patternOffset + step;
                
                if (patternStep < pattern.Steps.Length && pattern.Steps[patternStep] != -1)
                {
                    int note = pattern.Steps[patternStep];
                    int velocity = pattern.Velocities[patternStep];
                    int duration = pattern.Durations[patternStep];
                    
                    // Calculate absolute position
                    int absoluteStep = currentBar * stepsPerBar + step;
                    
                    // Add the note
                    MusicNote musicNote = new MusicNote
                    {
                        Note = note,
                        Velocity = velocity,
                        StartStep = absoluteStep,
                        Duration = duration,
                        Channel = part.Channel
                    };
                    
                    part.Notes.Add(musicNote);
                }
            }
        }

        private static void GeneratePercussionNotes(TrackPart part, List<PercussionPattern> patterns, int totalBars, int stepsPerBar)
        {
            if (patterns.Count == 0) return;
            
            // Clear existing notes
            part.Notes.Clear();
            
            // Get pattern length
            PercussionPattern firstPattern = patterns[0];
            int patternBars = firstPattern.KickSteps.Length / stepsPerBar;
            
            // MIDI drum note mappings
            int kickNote = 36;    // Bass Drum 1
            int snareNote = 38;   // Acoustic Snare
            int hihatNote = 42;   // Closed Hi-hat
            int crashNote = 49;   // Crash Cymbal 1
            int tomNote = 45;     // Low Tom
            int percNote = 76;    // High Wood Block
            
            // Fill track with patterns
            for (int bar = 0; bar < totalBars; bar++)
            {
                // Select pattern based on position and style
                int patternIndex;
                
                if (bar < totalBars / 4) // First quarter - main pattern
                {
                    patternIndex = 0;
                }
                else if ((bar + 1) % 4 == 0) // Every 4th bar (end of phrase) - fill pattern
                {
                    patternIndex = patterns.Count - 1; // Fill pattern
                }
                else if (bar >= totalBars - 4) // Last 4 bars - intense pattern
                {
                    // Alternate between main and fill for final buildup
                    patternIndex = bar % 2 == 0 ? patterns.Count - 1 : 1;
                }
                else // Middle section - mix of patterns
                {
                    double randomFactor = _intensity * 0.5;
                    
                    if (_rng.NextDouble() < randomFactor)
                    {
                        // Use a variation
                        patternIndex = 1 + _rng.Next(patterns.Count - 2);
                    }
                    else
                    {
                        // Use main pattern
                        patternIndex = 0;
                    }
                }
                
                // Apply the selected pattern for this bar
                PercussionPattern selectedPattern = patterns[patternIndex];
                int patternOffset = (bar % patternBars) * stepsPerBar;
                
                for (int step = 0; step < stepsPerBar; step++)
                {
                    int patternStep = patternOffset + step;
                    if (patternStep >= selectedPattern.KickSteps.Length) continue;
                    
                    // Calculate absolute position
                    int absoluteStep = bar * stepsPerBar + step;
                    
                    // Add kick drum notes
                    if (selectedPattern.KickSteps[patternStep])
                    {
                        part.Notes.Add(new MusicNote
                        {
                            Note = kickNote,
                            Velocity = selectedPattern.Velocities[patternStep, 0],
                            StartStep = absoluteStep,
                            Duration = 1, // Short for percussion
                            Channel = part.Channel
                        });
                    }
                    
                    // Add snare notes
                    if (selectedPattern.SnareSteps[patternStep])
                    {
                        part.Notes.Add(new MusicNote
                        {
                            Note = snareNote,
                            Velocity = selectedPattern.Velocities[patternStep, 1],
                            StartStep = absoluteStep,
                            Duration = 1,
                            Channel = part.Channel
                        });
                    }
                    
                    // Add hi-hat notes
                    if (selectedPattern.HihatSteps[patternStep])
                    {
                        part.Notes.Add(new MusicNote
                        {
                            Note = hihatNote,
                            Velocity = selectedPattern.Velocities[patternStep, 2],
                            StartStep = absoluteStep,
                            Duration = 1,
                            Channel = part.Channel
                        });
                    }
                    
                    // Add crash notes
                    if (selectedPattern.CrashSteps[patternStep])
                    {
                        part.Notes.Add(new MusicNote
                        {
                            Note = crashNote,
                            Velocity = selectedPattern.Velocities[patternStep, 3],
                            StartStep = absoluteStep,
                            Duration = 2, // Slightly longer for crash
                            Channel = part.Channel
                        });
                    }
                    
                    // Add tom notes
                    if (selectedPattern.TomSteps[patternStep])
                    {
                        part.Notes.Add(new MusicNote
                        {
                            Note = tomNote,
                            Velocity = selectedPattern.Velocities[patternStep, 4],
                            StartStep = absoluteStep,
                            Duration = 1,
                            Channel = part.Channel
                        });
                    }
                    
                    // Add percussion notes
                    if (selectedPattern.PercussionSteps[patternStep])
                    {
                        part.Notes.Add(new MusicNote
                        {
                            Note = percNote,
                            Velocity = selectedPattern.Velocities[patternStep, 5],
                            StartStep = absoluteStep,
                            Duration = 1,
                            Channel = part.Channel
                        });
                    }
                }
            }
        }

        private static void GenerateNotesFromArpeggioPatterns(TrackPart part, List<ArpeggioPattern> patterns, int totalBars, int stepsPerBar)
        {
            if (patterns.Count == 0) return;
            
            // Clear existing notes
            part.Notes.Clear();
            
            // Get pattern length
            ArpeggioPattern firstPattern = patterns[0];
            int patternBars = firstPattern.Notes.Length / stepsPerBar;
            
            // Fill track with patterns
            for (int bar = 0; bar < totalBars; bar++)
            {
                // Select pattern based on position
                int patternIndex;
                
                if (bar < totalBars / 4) // First quarter - main pattern
                {
                    patternIndex = 0;
                }
                else if (bar >= totalBars - 8 && bar < totalBars - 4) // Build up section
                {
                    patternIndex = 1; // Usually more intense variation
                }
                else if (bar >= totalBars - 4) // Last 4 bars - glitchy pattern
                {
                    patternIndex = patterns.Count - 1; 
                }
                else // Middle section - mix of patterns
                {
                    double randomFactor = _complexity * 0.6;
                    
                    if (_rng.NextDouble() < randomFactor)
                    {
                        // Use a variation
                        patternIndex = 1 + _rng.Next(patterns.Count - 2);
                    }
                    else
                    {
                        // Use main pattern
                        patternIndex = 0;
                    }
                }
                
                // Apply the selected pattern for this bar
                ArpeggioPattern selectedPattern = patterns[patternIndex];
                int patternOffset = (bar % patternBars) * stepsPerBar;
                
                for (int step = 0; step < stepsPerBar; step++)
                {
                    int patternStep = patternOffset + step;
                    if (patternStep >= selectedPattern.Notes.Length) continue;
                    
                    if (selectedPattern.Notes[patternStep] != -1)
                    {
                        int note = selectedPattern.Notes[patternStep];
                        int velocity = selectedPattern.Velocities[patternStep];
                        int duration = selectedPattern.Durations[patternStep];
                        
                        // Calculate absolute position
                        int absoluteStep = bar * stepsPerBar + step;
                        
                        // Add the note
                        MusicNote musicNote = new MusicNote
                        {
                            Note = note,
                            Velocity = velocity,
                            StartStep = absoluteStep,
                            Duration = duration,
                            Channel = part.Channel
                        };
                        
                        part.Notes.Add(musicNote);
                    }
                }
            }
        }

        private static void GenerateNotesFromPadPatterns(TrackPart part, List<PadPattern> patterns, int totalBars, int stepsPerBar)
        {
            if (patterns.Count == 0) return;
            
            // Clear existing notes
            part.Notes.Clear();
            
            // Get pattern length
            PadPattern firstPattern = patterns[0];
            
            // Estimate pattern bars from first chord duration
            int patternBars = 4; // Default
            if (firstPattern.StartSteps.Count > 0 && firstPattern.StartSteps.Count > 1)
            {
                patternBars = (firstPattern.StartSteps[1] - firstPattern.StartSteps[0]) / stepsPerBar;
            }
            
            // Fill track with patterns
            for (int bar = 0; bar < totalBars; bar += patternBars)
            {
                // Select pattern based on position
                int patternIndex;
                
                if (bar < totalBars / 3) // First third - main pad
                {
                    patternIndex = 0;
                }
                else if (bar >= totalBars - patternBars * 2) // Last section - tension
                {
                    patternIndex = 2; // Tension pattern
                }
                else // Middle section - mix of patterns
                {
                    // Alternate between main and variation
                    patternIndex = (bar / patternBars) % 2;
                }
                
                // Apply the selected pattern for this bar
                PadPattern selectedPattern = patterns[patternIndex];
                
                // Apply each chord in the pad pattern
                for (int chordIndex = 0; chordIndex < selectedPattern.StartSteps.Count; chordIndex++)
                {
                    int patternStartStep = selectedPattern.StartSteps[chordIndex];
                    int duration = selectedPattern.Durations[chordIndex];
                    int velocity = selectedPattern.Velocities[chordIndex];
                    int[] chordNotes = selectedPattern.Notes[chordIndex];
                    
                    // Calculate absolute position
                    int absoluteStartStep = bar * stepsPerBar + (patternStartStep % (patternBars * stepsPerBar));
                    
                    if (absoluteStartStep >= totalBars * stepsPerBar) continue;
                    
                    // Add notes for each chord tone
                    foreach (int note in chordNotes)
                    {
                        // Add the note
                        MusicNote musicNote = new MusicNote
                        {
                            Note = note,
                            Velocity = velocity,
                            StartStep = absoluteStartStep,
                            Duration = duration,
                            Channel = part.Channel
                        };
                        
                        part.Notes.Add(musicNote);
                    }
                }
            }
        }

        private static void GenerateNotesFromLeadPatterns(TrackPart part, List<LeadPattern> patterns, int totalBars, int stepsPerBar)
        {
            if (patterns.Count == 0) return;
            
            // Clear existing notes
            part.Notes.Clear();
            
            // Get pattern length
            LeadPattern firstPattern = patterns[0];
            int patternBars = firstPattern.Notes.Length / stepsPerBar;
            
            // Fill track with patterns
            for (int bar = 0; bar < totalBars; bar++)
            {
                // Select pattern based on position
                int patternIndex;
                
                // Have periods of melody and rest for a more natural sound
                if (bar < 4) // Intro - no lead
                {
                    continue;
                }
                else if (bar >= totalBars - 8) // Outro - motif pattern
                {
                    patternIndex = patterns.Count - 1;
                }
                else if ((bar / 4) % 3 == 2) // Every third 4-bar phrase - rest
                {
                    continue;
                }
                else // Normal sections - main patterns
                {
                    if (_rng.NextDouble() < 0.7)
                    {
                        patternIndex = (bar / 4) % (patterns.Count - 1);
                    }
                    else
                    {
                        patternIndex = patterns.Count - 1; // Motif
                    }
                }
                
                // Apply the selected pattern for this bar
                LeadPattern selectedPattern = patterns[patternIndex];
                int patternOffset = (bar % patternBars) * stepsPerBar;
                
                for (int step = 0; step < stepsPerBar; step++)
                {
                    int patternStep = patternOffset + step;
                    if (patternStep >= selectedPattern.Notes.Length) continue;
                    
                    if (selectedPattern.Notes[patternStep] != -1)
                    {
                        int note = selectedPattern.Notes[patternStep];
                        int velocity = selectedPattern.Velocities[patternStep];
                        int duration = selectedPattern.Durations[patternStep];
                        
                        // Calculate absolute position
                        int absoluteStep = bar * stepsPerBar + step;
                        
                        // Add the note
                        MusicNote musicNote = new MusicNote
                        {
                            Note = note,
                            Velocity = velocity,
                            StartStep = absoluteStep,
                            Duration = duration,
                            Channel = part.Channel
                        };
                        
                        part.Notes.Add(musicNote);
                    }
                }
            }
        }

        private static void GenerateNotesFromGlitchPatterns(TrackPart part, List<GlitchPattern> patterns, int totalBars, int stepsPerBar)
        {
            if (patterns.Count == 0) return;
            
            // Clear existing notes
            part.Notes.Clear();
            
            // Get pattern length
            GlitchPattern firstPattern = patterns[0];
            int patternBars = firstPattern.Notes.Length / stepsPerBar;
            
            // Glitch effects should be sparsely distributed
            int effectsFrequency = (int)(4 / _intensity);
            effectsFrequency = Math.Max(2, effectsFrequency); // At least every 2 bars
            
            // Fill track with patterns
            for (int bar = 0; bar < totalBars; bar++)
            {
                // Only apply glitch effects occasionally
                if (bar % effectsFrequency != 0 && bar < totalBars - 4) continue;
                
                // Select pattern based on position
                int patternIndex;
                
                if (bar >= totalBars - 4) // Last 4 bars - drop pattern
                {
                    patternIndex = patterns.Count - 1;
                }
                else if (bar % 8 == 0) // On major section boundaries - main pattern
                {
                    patternIndex = 0;
                }
                else // Other positions - random variations
                {
                    patternIndex = _rng.Next(patterns.Count - 1);
                }
                
                // Apply the selected pattern for this bar
                GlitchPattern selectedPattern = patterns[patternIndex];
                int patternOffset = (bar % patternBars) * stepsPerBar;
                
                for (int step = 0; step < stepsPerBar; step++)
                {
                    int patternStep = patternOffset + step;
                    if (patternStep >= selectedPattern.Notes.Length) continue;
                    
                    if (selectedPattern.Notes[patternStep] != -1)
                    {
                        int note = selectedPattern.Notes[patternStep];
                        int velocity = selectedPattern.Velocities[patternStep];
                        int duration = selectedPattern.Durations[patternStep];
                        GlitchEffect effect = selectedPattern.Effects[patternStep];
                        
                        // Calculate absolute position
                        int absoluteStep = bar * stepsPerBar + step;
                        
                        // Add the note
                        MusicNote musicNote = new MusicNote
                        {
                            Note = note,
                            Velocity = velocity,
                            StartStep = absoluteStep,
                            Duration = duration,
                            Channel = part.Channel,
                            Effect = effect
                        };
                        
                        part.Notes.Add(musicNote);
                    }
                }
            }
        }

        private static void GenerateSynthParameters(MusicTrack track)
        {
            // Generate synth parameters for each part
            foreach (var part in track.Parts)
            {
                SynthParameters parameters = new SynthParameters();

                if (_selectedSynthGeneration == 1) // synthwave
                {
                    Console.WriteLine("Generating Synthwave parameters");
                    GenerateSynthwaveParameters(part, parameters, track);
                }
                else // default
                {
                    switch (part.Type)
                    {
                        case TrackPartType.Bass:
                            GenerateBassSynthParameters(parameters);
                            break;
                    
                        case TrackPartType.Percussion:
                            GeneratePercussionSynthParameters(parameters);
                            break;
                    
                        case TrackPartType.Arpeggio:
                            GenerateArpeggioSynthParameters(parameters);
                            break;
                    
                        case TrackPartType.Pad:
                            GeneratePadSynthParameters(parameters, track);
                            break;
                    
                        case TrackPartType.Lead:
                            GenerateLeadSynthParameters(parameters);
                            break;
                    
                        case TrackPartType.GlitchFx:
                            GenerateGlitchSynthParameters(parameters);
                            break;
                    }
                }
                
                part.SynthParams = parameters;
            }
        }

        // TODO: more slow and continuous waves + synthesizer
        private static void GenerateSynthwaveParameters(TrackPart part, SynthParameters parameters, MusicTrack track)
        {
            switch (part.Type)
            {
                case TrackPartType.Bass:
                    parameters.Oscillators.Add(new WaveParameters
                    {
                        WaveType = WaveType.Sine,
                        Amplitude = 0.9f,
                        Envelope = new EnvelopeParameters
                        {
                            AttackTime = 0.1f,
                            DecayTime = 1.5f,
                            SustainLevel = 0.8f,
                            ReleaseTime = 2f
                        },
                        Filter = new FilterParameters
                        {
                            Type = FilterType.LowPass,
                            Cutoff = 300f
                        }
                    });
                    break;
                case TrackPartType.Percussion:
                    parameters.Oscillators.Add(new WaveParameters
                    {
                        WaveType = WaveType.Sine,
                        Amplitude = 0.7f,
                        Envelope = new EnvelopeParameters
                        {
                            AttackTime = 0.005f,
                            DecayTime = 0.2f,
                            SustainLevel = 0f,
                            ReleaseTime = 0.1f
                        }
                    });
                    break;
                case TrackPartType.Arpeggio:
                    parameters.Oscillators.Add(new WaveParameters
                    {
                        WaveType = WaveType.Square,
                        Amplitude = 0.6f,
                        Envelope = new EnvelopeParameters
                        {
                            AttackTime = 0.05f,
                            DecayTime = 0.2f,
                            SustainLevel = 0.7f,
                            ReleaseTime = 1.5f
                        },
                        Filter = new FilterParameters
                        {
                            Type = FilterType.BandPass,
                            Cutoff = 2500f,
                            Resonance = 0.5f
                        }
                    });
                    break;
                case TrackPartType.Lead:
                    parameters.Oscillators.Add(new WaveParameters
                    {
                        WaveType = WaveType.Square,
                        Amplitude = 0.7f,
                        Envelope = new EnvelopeParameters
                        {
                            AttackTime = 0.05f,
                            DecayTime = 0.3f,
                            SustainLevel = 0.6f,
                            ReleaseTime = 2.5f
                        },
                        Filter = new FilterParameters
                        {
                            Type = FilterType.LowPass,
                            Cutoff = 3200f,
                            Resonance = 0.4f
                        }
                    });
                    break;
                case TrackPartType.Pad:
                    parameters.Oscillators.Add(new WaveParameters
                    {
                        WaveType = WaveType.Sawtooth,
                        Amplitude = 0.5f,
                        Envelope = new EnvelopeParameters
                        {
                            AttackTime = 0.8f,
                            DecayTime = 1f,
                            SustainLevel = 0.7f,
                            ReleaseTime = 4f
                        },
                        Filter = new FilterParameters
                        {
                            Type = FilterType.LowPass,
                            Cutoff = 400f,
                            Resonance = 0.2f
                        }
                    });
                    break;
                case TrackPartType.GlitchFx:
                    parameters.Oscillators.Add(new WaveParameters
                    {
                        WaveType = WaveType.Triangle,
                        Amplitude = 0.6f,
                        Envelope = new EnvelopeParameters
                        {
                            AttackTime = 0.001f,
                            DecayTime = 0.1f,
                            SustainLevel = 0.3f,
                            ReleaseTime = 0.1f
                        },
                        Filter = new FilterParameters
                        {
                            Type = FilterType.BandPass,
                            Cutoff = 2000f,
                            Resonance = 0.8f
                        }
                    });
                    break;
            }
        }

        private static void GenerateBassSynthParameters(SynthParameters parameters)
        {
            parameters.Oscillators.Clear();
            
            // Main oscillator
            WaveParameters main = new WaveParameters
            {
                WaveType = WaveType.Square,
                Amplitude = 0.8f,
                IsEnabled = true,
                Envelope = new EnvelopeParameters
                {
                    AttackTime = 0.01f,
                    DecayTime = 0.2f,
                    SustainLevel = 0.6f,
                    ReleaseTime = 0.3f
                },
                // Filter
                Filter = new FilterParameters
                {
                    Type = FilterType.LowPass,
                    Cutoff = 200 + _intensity * 800, // deeper cutoff
                    Resonance = 0.3f
                }
            };

            if (_includeBitCrusher)
            {
                parameters.Effects.Add(new EffectParameters
                {
                    Type = EffectType.BitCrusher,
                    Amount = 0.15f
                });
            }
            
            parameters.Oscillators.Add(main);
        }

        private static void GeneratePercussionSynthParameters(SynthParameters parameters)
        {
            // Percussion uses samples, so minimal synth parameters needed
            parameters.Oscillators.Clear();
            
            // Generic percussion settings
            WaveParameters perc = new WaveParameters
            {
                WaveType = WaveType.Triangle,
                Amplitude = 0.9f,
                IsEnabled = true,
                // Short, punchy envelope
                Envelope = new EnvelopeParameters
                {
                    AttackTime = 0.001f,
                    DecayTime = 0.2f,
                    SustainLevel = 0.0f,
                    ReleaseTime = 0.1f
                }
            };

            parameters.Oscillators.Add(perc);
            
            // Effects
            if (_includeReverb)
            {
                parameters.Effects.Add(new EffectParameters
                {
                    Type = EffectType.Reverb,
                    Amount = 0.2f
                });
            }
            
            if (_intensity > 0.7 && _includeDistortion)
            {
                parameters.Effects.Add(new EffectParameters
                {
                    Type = EffectType.Distortion,
                    Amount = 0.1f + _intensity * 0.1f
                });
            }
        }

        private static void GenerateArpeggioSynthParameters(SynthParameters parameters)
        {
            parameters.Oscillators.Clear();
            
            // Main oscillator - typically square wave
            WaveParameters main = new WaveParameters
            {
                WaveType = WaveType.Square,
                Amplitude = 0.6f,
                IsEnabled = true,
                Envelope = new EnvelopeParameters
                {
                    AttackTime = 0.01f,
                    DecayTime = 0.1f,
                    SustainLevel = 0.4f,
                    ReleaseTime = 0.1f
                },
                Filter = new FilterParameters
                {
                    Type = FilterType.BandPass,
                    Cutoff = 3000,
                    Resonance = 0.5f
                }
            };
            parameters.Oscillators.Add(main);
            
            // Secondary oscillator for richness
            if (_complexity > 0.5)
            {
                WaveParameters second = new WaveParameters
                {
                    WaveType = WaveType.Sawtooth,
                    Amplitude = 0.3f,
                    FrequencyOffset = 7, // Perfect fifth
                    IsEnabled = true
                };
                
                // ADSR - similar to main
                second.Envelope = new EnvelopeParameters
                {
                    AttackTime = 0.01f,
                    DecayTime = 0.15f,
                    SustainLevel = 0.3f,
                    ReleaseTime = 0.1f
                };
                
                parameters.Oscillators.Add(second);
            }
            
            // Effects
            if (_includeDelay)
            {
                parameters.Effects.Add(new EffectParameters
                {
                    Type = EffectType.Delay,
                    Amount = 0.3f + _complexity * 0.2f
                });
            }
            
            if (_includeReverb)
            {
                parameters.Effects.Add(new EffectParameters
                {
                    Type = EffectType.Reverb,
                    Amount = 0.2f
                });
            }
        }

        private static void GeneratePadSynthParameters(SynthParameters parameters, MusicTrack track)
        {
            parameters.Oscillators.Clear();
            
            // Main oscillator
            WaveParameters main = new WaveParameters
            {
                WaveType = WaveType.Sawtooth,
                Amplitude = 0.5f,
                IsEnabled = true,
                // ADSR - slow attack and release for pads
                Envelope = new EnvelopeParameters
                {
                    AttackTime = 0.5f + (1 - _intensity) * 0.5f,
                    DecayTime = 0.3f,
                    SustainLevel = 0.7f,
                    ReleaseTime = 1.0f + (1 - _intensity) * 1.0f
                },
                // Filter - darker for pads
                Filter = new FilterParameters
                {
                    Type = FilterType.LowPass,
                    Cutoff = 400 + _complexity * 1200,
                    Resonance = 0.2f
                }
            };

            parameters.Oscillators.Add(main);
            
            // Secondary oscillator for richness
            WaveParameters second = new WaveParameters
            {
                WaveType = WaveType.Sine,
                Amplitude = 0.4f,
                FrequencyOffset = track.IsMinor ? 3 : 4, // Third
                IsEnabled = true,
                // ADSR - slower attack than main
                Envelope = new EnvelopeParameters
                {
                    AttackTime = 0.8f,
                    DecayTime = 0.4f,
                    SustainLevel = 0.6f,
                    ReleaseTime = 1.2f
                }
            };

            parameters.Oscillators.Add(second);
            
            // Effects - pads need reverb and chorus
            parameters.Effects.Add(new EffectParameters
            {
                Type = EffectType.Chorus,
                Amount = 0.3f
            });
            
            if (_includeReverb)
            {
                parameters.Effects.Add(new EffectParameters
                {
                    Type = EffectType.Reverb,
                    Amount = 0.6f + (1 - _intensity) * 0.3f
                });
            }
        }

        private static void GenerateLeadSynthParameters(SynthParameters parameters)
        {
            parameters.Oscillators.Clear();
            
            // Main oscillator
            WaveParameters main = new WaveParameters
            {
                WaveType = _intensity > 0.6 ? WaveType.Sawtooth : WaveType.Square,
                Amplitude = 0.7f,
                IsEnabled = true,
                Envelope = new EnvelopeParameters
                {
                    AttackTime = 0.05f,
                    DecayTime = 0.2f,
                    SustainLevel = 0.6f,
                    ReleaseTime = 0.3f
                },
                Filter = new FilterParameters
                {
                    Type = FilterType.LowPass,
                    Cutoff = 3000 + _intensity * 2000,
                    Resonance = 0.4f + _intensity * 0.3f
                }
            };

            parameters.Oscillators.Add(main);

            WaveParameters modulator = new WaveParameters
            {
                WaveType = WaveType.Square,
                Amplitude = 0.4f,
                FrequencyOffset = 7,
                IsEnabled = true
            };
            parameters.Oscillators.Add(modulator);
            
            // Secondary oscillator for richness
            if (_complexity > 0.4)
            {
                WaveParameters second = new WaveParameters
                {
                    WaveType = WaveType.Square,
                    Amplitude = 0.3f,
                    FrequencyOffset = 12, // Octave
                    IsEnabled = true,
                    // ADSR - similar to main
                    Envelope = new EnvelopeParameters
                    {
                        AttackTime = 0.08f,
                        DecayTime = 0.3f,
                        SustainLevel = 0.5f,
                        ReleaseTime = 0.3f
                    }
                };

                parameters.Oscillators.Add(second);
            }
            
            // Effects
            if (_includeDelay)
            {
                parameters.Effects.Add(new EffectParameters
                {
                    Type = EffectType.Delay,
                    Amount = 0.25f
                });
            }
            
            if (_includeReverb)
            {
                parameters.Effects.Add(new EffectParameters
                {
                    Type = EffectType.Reverb,
                    Amount = 0.3f
                });
            }
            
            // Distortion for more aggressive leads
            if (_intensity > 0.6 && _includeDistortion)
            {
                parameters.Effects.Add(new EffectParameters
                {
                    Type = EffectType.Distortion,
                    Amount = _intensity * 0.3f
                });
            }
        }

        private static void GenerateGlitchSynthParameters(SynthParameters parameters)
        {
            parameters.Oscillators.Clear();
            
            // Main oscillator - typically noisy
            WaveParameters main = new WaveParameters
            {
                WaveType = _rng.NextDouble() < 0.5 ? WaveType.Triangle : WaveType.Square,
                Amplitude = 0.6f,
                IsEnabled = true,
                // ADSR - very quick for glitches
                Envelope = new EnvelopeParameters
                {
                    AttackTime = 0.001f,
                    DecayTime = 0.1f,
                    SustainLevel = 0.3f,
                    ReleaseTime = 0.1f
                },
                // Filter - varies based on effect type
                Filter = new FilterParameters
                {
                    Type = FilterType.BandPass,
                    Cutoff = 2000,
                    Resonance = 0.8f
                }
            };

            parameters.Oscillators.Add(main);
            
            // Effects - heavy processing
            if (_includeBitCrusher)
            {
                parameters.Effects.Add(new EffectParameters
                {
                    Type = EffectType.BitCrusher,
                    Amount = 0.7f
                });
            }

            if (_includeDistortion)
            {
                parameters.Effects.Add(new EffectParameters
                {
                    Type = EffectType.Distortion,
                    Amount = 0.5f + _intensity * 0.3f
                });
            }
            
            if (_includeDelay)
            {
                parameters.Effects.Add(new EffectParameters
                {
                    Type = EffectType.Delay,
                    Amount = 0.4f
                });
            }
        }

        private static void InitializeAudioSystem()
        {
            if (_isInitialized) return;

            _waveOut = new WaveOutEvent();
            _synth = new StreamingSynthesizer((Style) _selectedSynthStyle);
            _waveOut.Init(_synth);

            _isInitialized = true;
        }
        
        private static void StartPlayback()
        {
            if (_currentTrack == null) return;

            // Ensure any previous playback is stopped
            StopPlayback(true);

            try
            {
                _synth = new StreamingSynthesizer((Style) _selectedSynthStyle);
                _synth.LoadTrack(_currentTrack, _reverbMix, _reverbTime, _delayMix, _delayFeedback, _delayTime, 
                    _bitCrusherReduction, _distortionDrive, _distortionPostGain, _isLooping);
                
                _waveOut = new WaveOutEvent();
                _waveOut.Init(_synth);
                
                _waveOut.Play();
                _isPlaying = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting playback: {ex.Message}");
                StopPlayback(true);
            }
        }

        private static void StopPlayback(bool fullCleanup = false)
        {
            if (!_isPlaying && !fullCleanup) return;
            
            try
            {
                _isPlaying = false;
                
                if (_waveOut != null)
                {
                    try
                    {
                        _waveOut.Stop();
                        
                        if (fullCleanup)
                        {
                            _waveOut.Dispose();
                            _waveOut = null;
                            _synth = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error stopping audio: {ex.Message}");
                        _waveOut = null;
                        _synth = null;
                    }
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in StopPlayback: {ex.Message}");
            }
        }

        private static void RenderTrackVisualization()
        {
            if (_currentTrack == null) return;
            
            // Draw a simple visualization of the track
            ImGui.Text("Track Overview:");
            
            float barWidth = 10;
            float trackHeight = 120;
            float partHeight = 15;
            
            Vector2 startPos = ImGui.GetCursorScreenPos();
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            
            // Calculate visualization dimensions
            float width = Math.Min(ImGui.GetContentRegionAvail().X, _currentTrack.BarCount * barWidth);
            ImGui.InvisibleButton("trackVis", new Vector2(width, trackHeight));
            
            // Draw background
            drawList.AddRectFilled(
                startPos,
                new Vector2(startPos.X + width, startPos.Y + trackHeight),
                ImGui.ColorConvertFloat4ToU32(new Vector4(0.1f, 0.1f, 0.1f, 1.0f))
            );
            
            // Draw bar lines
            for (int bar = 0; bar <= _currentTrack.BarCount; bar++)
            {
                float x = startPos.X + bar * barWidth;
                drawList.AddLine(
                    new Vector2(x, startPos.Y),
                    new Vector2(x, startPos.Y + trackHeight),
                    ImGui.ColorConvertFloat4ToU32(new Vector4(0.3f, 0.3f, 0.3f, 1.0f))
                );
            }
            
            // Draw parts
            float yOffset = 5;
            foreach (var part in _currentTrack.Parts)
            {
                // Skip empty parts
                if (part.Notes.Count == 0) continue;
                
                // Choose color based on part type
                Vector4 color;
                switch (part.Type)
                {
                    case TrackPartType.Bass:
                        color = new Vector4(0.8f, 0.2f, 0.2f, 0.8f); // Red
                        break;
                    case TrackPartType.Percussion:
                        color = new Vector4(0.2f, 0.2f, 0.8f, 0.8f); // Blue
                        break;
                    case TrackPartType.Arpeggio:
                        color = new Vector4(0.2f, 0.8f, 0.2f, 0.8f); // Green
                        break;
                    case TrackPartType.Pad:
                        color = new Vector4(0.8f, 0.2f, 0.8f, 0.8f); // Purple
                        break;
                    case TrackPartType.Lead:
                        color = new Vector4(0.8f, 0.8f, 0.2f, 0.8f); // Yellow
                        break;
                    case TrackPartType.GlitchFx:
                        color = new Vector4(0.8f, 0.5f, 0.2f, 0.8f); // Orange
                        break;
                    default:
                        color = new Vector4(0.8f, 0.8f, 0.8f, 0.8f); // White
                        break;
                }
                
                // Draw the part label
                drawList.AddText(
                    new Vector2(startPos.X + 5, startPos.Y + yOffset),
                    ImGui.ColorConvertFloat4ToU32(color),
                    part.Type.ToString()
                );
                
                // Group notes by bar for better visualization
                Dictionary<int, int> noteCountByBar = new Dictionary<int, int>();
                for (int bar = 0; bar < _currentTrack.BarCount; bar++)
                {
                    noteCountByBar[bar] = 0;
                }
                
                foreach (var note in part.Notes)
                {
                    int bar = note.StartStep / 16;
                    if (bar < _currentTrack.BarCount)
                    {
                        noteCountByBar[bar]++;
                    }
                }
                
                // Draw activity bars
                for (int bar = 0; bar < _currentTrack.BarCount; bar++)
                {
                    if (noteCountByBar[bar] > 0)
                    {
                        // Calculate bar height based on note density
                        float intensity = Math.Min(1.0f, noteCountByBar[bar] / 16.0f);
                        float height = partHeight * intensity;
                        
                        drawList.AddRectFilled(
                            new Vector2(startPos.X + bar * barWidth + 1, startPos.Y + yOffset + 14 - height),
                            new Vector2(startPos.X + (bar + 1) * barWidth - 1, startPos.Y + yOffset + 14),
                            ImGui.ColorConvertFloat4ToU32(color)
                        );
                    }
                }
                
                yOffset += partHeight + 5;
            }
            
            // Draw playback position
            if (_isPlaying && _synth != null)
            {
                float playbackX = startPos.X + (_synth.CurrentBar + _synth.StepInBar / 16.0f) * barWidth;
                
                drawList.AddLine(
                    new Vector2(playbackX, startPos.Y),
                    new Vector2(playbackX, startPos.Y + trackHeight),
                    ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)),
                    2.0f
                );
            }
        }
        
        private static void ExportTrackToAudio(MusicTrack track, string format = "mp3")
        {
            if (track == null) return;
            
            string outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Blastia AI", 
                $"{track.Name}.{format}");
                
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            
            // Create an in-memory audio renderer
            WaveFormat waveFormat = new WaveFormat(44100, 16, 2);
            string tempWavPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".wav");
            _exportStatus = "Exporting Temp .wav file";
            
            using (var fileStream = new FileStream(tempWavPath, FileMode.Create))
            {
                RenderAudioToStream(track, fileStream, waveFormat);
            }

            _exportStatus = $"Converting to .{format}";

            if (format.ToLower() == "wav")
            {
                File.Copy(tempWavPath, outputPath, true);
            }
            else
            {
                using (var reader = new AudioFileReader(tempWavPath))
                {
                    if (format.ToLower() == "mp3")
                    {
                        MediaFoundationEncoder.EncodeToMp3(reader, outputPath);
                    }
                    // OGG doesnt work
                    // else if (format.ToLower() == "ogg")
                    // {
                    //     MediaFoundationEncoder.EncodeToWma(reader, outputPath);
                    // }
                }
            }
            
            _exportStatus = $"Exported to {outputPath}";
        }

        private static void RenderAudioToStream(MusicTrack track, FileStream fileStream, WaveFormat waveFormat)
        {
            using (var writer = new WaveFileWriter(fileStream, waveFormat))
            {
                var synth = new StreamingSynthesizer((Style) _selectedSynthStyle);
                synth.LoadTrack(track, _reverbMix, _reverbTime, _delayMix, _delayFeedback, _delayTime, 
                    _bitCrusherReduction, _distortionDrive, _distortionPostGain);
        
                // Calculate SAMPLES PER STEP (not frames)
                double secondsPerStep = (60000.0 / track.Tempo / 4.0) / 1000.0;
                double samplesPerStep = secondsPerStep * waveFormat.SampleRate;
        
                // Total SAMPLES = steps * samples/step * channels
                int totalSamples = (int)Math.Round(track.BarCount * 16 * samplesPerStep) * waveFormat.Channels;
        
                // Buffer size in SAMPLES (not frames)
                int bufferSize = (int)(waveFormat.SampleRate * 0.1) * waveFormat.Channels;
                float[] sampleBuffer = new float[bufferSize];
        
                int samplesRendered = 0;
                while (samplesRendered < totalSamples)
                {
                    int samplesToRead = Math.Min(bufferSize, totalSamples - samplesRendered);
                    int samplesRead = synth.Read(sampleBuffer, 0, samplesToRead);
            
                    if (samplesRead == 0) break;
            
                    // Convert to 16-bit PCM
                    byte[] byteBuffer = new byte[samplesRead * 2];
                    for (int i = 0; i < samplesRead; i++)
                    {
                        short sample = (short)(sampleBuffer[i] * short.MaxValue);
                        byteBuffer[i * 2] = (byte)(sample & 0xFF);
                        byteBuffer[i * 2 + 1] = (byte)(sample >> 8);
                    }
            
                    writer.Write(byteBuffer, 0, byteBuffer.Length);
                    samplesRendered += samplesRead;
                }
        
                writer.Flush();
            }
        }
        
        public static void Cleanup()
        {
            StopPlayback();
            
            if (_waveOut != null)
            {
                _waveOut.Dispose();
                _waveOut = null;
            }
        }
    }
    
    // Music data structures
    public class MusicTrack
    {
        public string Name { get; set; }
        public int Style { get; set; }
        public int Tempo { get; set; } = 120;
        public int BarCount { get; set; } = 16;
        public int Key { get; set; } // 0 = C, 1 = C#, etc.
        public bool IsMinor { get; set; } = true;
        public List<int[]> ChordProgression { get; set; } = [];
        public int BarsPerChord { get; set; } = 4;
        public List<TrackPart> Parts { get; set; } = [];
    }

    public class TrackPart
    {
        public TrackPartType Type { get; set; }
        public int Channel { get; set; }
        public int Program { get; set; }
        public List<Pattern> Patterns { get; set; } = [];
        public List<MusicNote> Notes { get; set; } = [];
        public SynthParameters SynthParams { get; set; } = new();
    }

    public class MusicNote
    {
        public int Note { get; set; }
        public int Velocity { get; set; }
        public int StartStep { get; set; }
        public int Duration { get; set; }
        public int Channel { get; set; }
        public GlitchEffect Effect { get; set; } = GlitchEffect.None;
    }

    // Pattern types
    public abstract class Pattern { }

    public class BassPattern : Pattern
    {
        public int[] Steps { get; set; } = [];
        public int[] Velocities { get; set; } = [];
        public int[] Durations { get; set; } = [];
    }

    public class PercussionPattern : Pattern
    {
        public bool[] KickSteps { get; set; } = [];
        public bool[] SnareSteps { get; set; } = [];
        public bool[] HihatSteps { get; set; } = [];
        public bool[] CrashSteps { get; set; } = [];
        public bool[] TomSteps { get; set; } = [];
        public bool[] PercussionSteps { get; set; } = [];
        public int[,] Velocities { get; set; } // [step, instrument]
    }

    public class ArpeggioPattern : Pattern
    {
        public int[] Notes { get; set; } = [];
        public int[] Velocities { get; set; } = [];
        public int[] Durations { get; set; } = [];
    }

    public class PadPattern : Pattern
    {
        public List<int[]> Notes { get; set; } = []; // Chord notes
        public List<int> StartSteps { get; set; } = [];
        public List<int> Durations { get; set; } = [];
        public List<int> Velocities { get; set; } = [];
    }

    public class LeadPattern : Pattern
    {
        public int[] Notes { get; set; } = [];
        public int[] Velocities { get; set; } = [];
        public int[] Durations { get; set; } = [];
    }

    public class GlitchPattern : Pattern
    {
        public int[] Notes { get; set; } = [];
        public int[] Velocities { get; set; } = [];
        public int[] Durations { get; set; } = [];
        public GlitchEffect[] Effects { get; set; } = [];
    }

    // Synth parameters
    public class SynthParameters
    {
        public List<WaveParameters> Oscillators { get; set; } = [];
        public List<EffectParameters> Effects { get; set; } = [];
        
        // synthwave-specific
        public float PadTailDuration { get; set; } = 4f; // seconds
        public bool UseTapeSaturation { get; set; }
        public float AnalogDriftAmount { get; set; } = 0.1f;
    }

    public enum WaveType
    {
        Sine, Square, Triangle, Sawtooth
    }
    
    public class WaveParameters
    {
        public WaveType WaveType { get; set; } = WaveType.Sine;
        public float Amplitude { get; set; } = 0.5f;
        public float FrequencyOffset { get; set; } // In semitones
        public bool IsEnabled { get; set; } = true;
        public EnvelopeParameters Envelope { get; set; } = new();
        public FilterParameters Filter { get; set; } = new();
    }

    public class EnvelopeParameters
    {
        public float AttackTime { get; set; } = 0.01f;
        public float DecayTime { get; set; } = 0.1f;
        public float SustainLevel { get; set; } = 0.7f;
        public float ReleaseTime { get; set; } = 0.3f;
    }

    public class FilterParameters
    {
        public FilterType Type { get; set; } = FilterType.LowPass;
        public float Cutoff { get; set; } = 1000f;
        public float Resonance { get; set; } = 0.5f;
    }

    public class EffectParameters
    {
        public EffectType Type { get; set; }
        public float Amount { get; set; } = 0.5f;
    }

    // Enums
    public enum TrackPartType
    {
        Bass,
        Percussion,
        Arpeggio,
        Pad,
        Lead,
        GlitchFx
    }

    public enum GlitchEffect
    {
        None,
        Stutter,
        DigitalNoise,
        PitchBend,
        FilterSweep,
        RiserFall,
        Distortion
    }

    public enum EffectType
    {
        Reverb,
        Delay,
        Chorus,
        Distortion,
        BitCrusher,
        Filter
    }
}