using System.Numerics;
using ImGuiNET;
using NAudio.Wave;
using SDL2;

namespace Blastia.Main.Synthesizer;

public class Synthesizer
{
    private static IntPtr _window;
    private static IntPtr _renderer;
    private static IntPtr _imGuiContext;
    
    // synth
    private static float _frequency = 444.0f;
    private static float _amplitude = 0.15f;
    private static int _currentWaveType = 0;
    private static bool _isPlaying = false;
    private static string[] _waveTypes =
    [
        "Sine",
        "Square",
        "Triangle",
        "Sawtooth"
    ];
    private static float[] _waveFormPoints = new float[600];
    
    // audio
    private static MultipleWaveSynth _synth;
    private static List<WaveData> _waves = [];
    private static WaveOutEvent _waveOut;
    private static uint _time;
    private static IntPtr _fontTexture;
    private static double _timeWindow = 0.015; // 15ms window to visualize
    
    private static bool _showAdsrHelp;
    private static int _selectedAdsrWaveIndex;
    
    private static bool _showFilterHelp;
    private static int _selectedFilterWaveIndex;

    private const int WINDOW_WIDTH = 1920;
    private const int WINDOW_HEIGHT = 1080;

    public static void Launch(string[] args)
    {
        if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) < 0)
        {
            Console.WriteLine($"Error: SDL Initialization Error: {SDL.SDL_GetError()}");
            return;
        }
        
        // create window
        _window = SDL.SDL_CreateWindow("Blastia Synthesizer", SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED,  WINDOW_WIDTH, WINDOW_HEIGHT, SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);
        if (_window == IntPtr.Zero)
        {
            Console.WriteLine($"Error creating SDL window: {SDL.SDL_GetError()}");
            return;
        }
        
        // create renderer
        _renderer = SDL.SDL_CreateRenderer(_window, -1, 
            SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED | SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);
        if (_renderer == IntPtr.Zero)
        {
            Console.WriteLine($"Error creating SDL renderer: {SDL.SDL_GetError()}");
            SDL.SDL_DestroyWindow(_window);
            SDL.SDL_Quit();
            return;
        }
        
         // Create ImGui context
        _imGuiContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(_imGuiContext);
        ImGui.StyleColorsDark();
        
        // Initialize ImGui for SDL2
        ImGuiIOPtr io = ImGui.GetIO();
        
        // CRUCIAL: Set display size for ImGui
        int windowWidth, windowHeight;
        SDL.SDL_GetWindowSize(_window, out windowWidth, out windowHeight);
        io.DisplaySize = new Vector2(windowWidth, windowHeight);
        
        // Create font texture
        CreateFontTexture();
        
        // Initialize audio
        InitializeAudio();

        // Main loop
        _time = (uint)SDL.SDL_GetTicks();
        bool running = true;
        while (running)
        {
            // Process events
            while (SDL.SDL_PollEvent(out SDL.SDL_Event e) != 0)
            {
                ProcessEvent(e);
                if (e.type == SDL.SDL_EventType.SDL_QUIT)
                {
                    running = false;
                }
            }
            
            // Prepare new frame
            NewFrame();
            
            // Render UI
            RenderUI();
            
            // Render
            ImGui.Render();
            SDL.SDL_SetRenderDrawColor(_renderer, 100, 100, 100, 255);
            SDL.SDL_RenderClear(_renderer);
            
            // Render ImGui draw data
            RenderDrawData(ImGui.GetDrawData());
            SDL.SDL_RenderPresent(_renderer);
            
            // Update waveform
            UpdateWaveformVisuals();
        }
        
        // Cleanup
        _waveOut?.Stop();
        _waveOut?.Dispose();
        
        if (_fontTexture != IntPtr.Zero)
        {
            SDL.SDL_DestroyTexture(_fontTexture);
            _fontTexture = IntPtr.Zero;
        }
        
        ImGui.DestroyContext();
        SDL.SDL_DestroyRenderer(_renderer);
        SDL.SDL_DestroyWindow(_window);
        SDL.SDL_Quit();
    }

    private static void CreateFontTexture()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        
        // Build font texture
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);
        
        // Create a texture for the font
        _fontTexture = SDL.SDL_CreateTexture(
            _renderer,
            SDL.SDL_PIXELFORMAT_ABGR8888,
            (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STATIC,
            width, height);
            
        if (_fontTexture == IntPtr.Zero)
        {
            Console.WriteLine($"Error creating font texture: {SDL.SDL_GetError()}");
            return;
        }
        
        // Update texture with font data
        SDL.SDL_UpdateTexture(_fontTexture, IntPtr.Zero, pixels, width * bytesPerPixel);
        
        // Set texture parameters
        SDL.SDL_SetTextureBlendMode(_fontTexture, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
        
        // Store texture ID in ImGui
        io.Fonts.SetTexID(_fontTexture);
        io.Fonts.ClearTexData();
    }
    
   private static void ProcessEvent(SDL.SDL_Event e)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        
        switch (e.type)
        {
            case SDL.SDL_EventType.SDL_MOUSEWHEEL:
                if (e.wheel.y > 0)
                    io.MouseWheel = 1;
                if (e.wheel.y < 0)
                    io.MouseWheel = -1;
                break;
                
            case SDL.SDL_EventType.SDL_MOUSEMOTION:
                io.MousePos = new Vector2(e.motion.x, e.motion.y);
                break;
                
            case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
            case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                bool pressed = e.type == SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN;
                if (e.button.button == SDL.SDL_BUTTON_LEFT)
                    io.MouseDown[0] = pressed;
                if (e.button.button == SDL.SDL_BUTTON_RIGHT)
                    io.MouseDown[1] = pressed;
                if (e.button.button == SDL.SDL_BUTTON_MIDDLE)
                    io.MouseDown[2] = pressed;
                break;
                
            case SDL.SDL_EventType.SDL_KEYDOWN:
            case SDL.SDL_EventType.SDL_KEYUP:
                bool down = e.type == SDL.SDL_EventType.SDL_KEYDOWN;
                
                // Update modifier states
                io.KeyShift = (SDL.SDL_GetModState() & SDL.SDL_Keymod.KMOD_SHIFT) != 0;
                io.KeyCtrl = (SDL.SDL_GetModState() & SDL.SDL_Keymod.KMOD_CTRL) != 0;
                io.KeyAlt = (SDL.SDL_GetModState() & SDL.SDL_Keymod.KMOD_ALT) != 0;
                
                // Map keys using the modern ImGui.NET API
                // Convert SDL key code to ImGui key code
                ImGuiKey imguiKey = SDLKeyToImGuiKey(e.key.keysym.sym);
                if (imguiKey != ImGuiKey.None)
                {
                    io.AddKeyEvent(imguiKey, down);
                }
                break;
        }
    }

    // Helper function to map SDL keys to ImGui keys
    private static ImGuiKey SDLKeyToImGuiKey(SDL.SDL_Keycode keycode)
    {
        switch (keycode)
        {
            case SDL.SDL_Keycode.SDLK_TAB: return ImGuiKey.Tab;
            case SDL.SDL_Keycode.SDLK_LEFT: return ImGuiKey.LeftArrow;
            case SDL.SDL_Keycode.SDLK_RIGHT: return ImGuiKey.RightArrow;
            case SDL.SDL_Keycode.SDLK_UP: return ImGuiKey.UpArrow;
            case SDL.SDL_Keycode.SDLK_DOWN: return ImGuiKey.DownArrow;
            case SDL.SDL_Keycode.SDLK_PAGEUP: return ImGuiKey.PageUp;
            case SDL.SDL_Keycode.SDLK_PAGEDOWN: return ImGuiKey.PageDown;
            case SDL.SDL_Keycode.SDLK_HOME: return ImGuiKey.Home;
            case SDL.SDL_Keycode.SDLK_END: return ImGuiKey.End;
            case SDL.SDL_Keycode.SDLK_INSERT: return ImGuiKey.Insert;
            case SDL.SDL_Keycode.SDLK_DELETE: return ImGuiKey.Delete;
            case SDL.SDL_Keycode.SDLK_BACKSPACE: return ImGuiKey.Backspace;
            case SDL.SDL_Keycode.SDLK_SPACE: return ImGuiKey.Space;
            case SDL.SDL_Keycode.SDLK_RETURN: return ImGuiKey.Enter;
            case SDL.SDL_Keycode.SDLK_ESCAPE: return ImGuiKey.Escape;
            case SDL.SDL_Keycode.SDLK_a: return ImGuiKey.A;
            case SDL.SDL_Keycode.SDLK_c: return ImGuiKey.C;
            case SDL.SDL_Keycode.SDLK_v: return ImGuiKey.V;
            case SDL.SDL_Keycode.SDLK_x: return ImGuiKey.X;
            case SDL.SDL_Keycode.SDLK_y: return ImGuiKey.Y;
            case SDL.SDL_Keycode.SDLK_z: return ImGuiKey.Z;
            default: return ImGuiKey.None;
        }
    }
    
    private static void NewFrame()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        
        // Update display size
        int windowWidth, windowHeight;
        SDL.SDL_GetWindowSize(_window, out windowWidth, out windowHeight);
        io.DisplaySize = new Vector2(windowWidth, windowHeight);
        
        // Update time delta
        uint currentTime = (uint)SDL.SDL_GetTicks();
        float deltaTime = (currentTime - _time) / 1000.0f;
        _time = currentTime;
        io.DeltaTime = deltaTime;
        
        // Update mouse state
        int mouseX, mouseY;
        uint buttons = SDL.SDL_GetMouseState(out mouseX, out mouseY);
        io.MousePos = new Vector2(mouseX, mouseY);
        
        // Mouse buttons (simplified - you may want to map these more carefully)
        io.MouseDown[0] = (buttons & SDL.SDL_BUTTON(SDL.SDL_BUTTON_LEFT)) != 0;
        io.MouseDown[1] = (buttons & SDL.SDL_BUTTON(SDL.SDL_BUTTON_RIGHT)) != 0;
        io.MouseDown[2] = (buttons & SDL.SDL_BUTTON(SDL.SDL_BUTTON_MIDDLE)) != 0;
        
        // Start the Dear ImGui frame
        ImGui.NewFrame();
    }
    
    private static void RenderDrawData(ImDrawDataPtr drawData)
    {
        // Skip if minimized
        if (drawData.DisplaySize.X <= 0.0f || drawData.DisplaySize.Y <= 0.0f)
            return;
        
        // For each command list
        for (int cmdListIdx = 0; cmdListIdx < drawData.CmdListsCount; cmdListIdx++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[cmdListIdx];
            
            // Get vertex/index buffers
            ImPtrVector<ImDrawVertPtr> vtxBuffer = cmdList.VtxBuffer;
            ImVector<ushort> idxBuffer = cmdList.IdxBuffer;
            
            // For each command in the command list
            for (int cmdIdx = 0; cmdIdx < cmdList.CmdBuffer.Size; cmdIdx++)
            {
                ImDrawCmdPtr cmd = cmdList.CmdBuffer[cmdIdx];
                
                // Set clip rectangle
                SDL.SDL_Rect r = new SDL.SDL_Rect
                {
                    x = (int)cmd.ClipRect.X,
                    y = (int)cmd.ClipRect.Y,
                    w = (int)(cmd.ClipRect.Z - cmd.ClipRect.X),
                    h = (int)(cmd.ClipRect.W - cmd.ClipRect.Y)
                };
                SDL.SDL_RenderSetClipRect(_renderer, ref r);
                
                // Check if we're using a texture (font or other)
                if (cmd.TextureId != IntPtr.Zero && cmd.TextureId == _fontTexture)
                {
                    // Text rendering - we need to use SDL_RenderGeometry
                    // For each triangle in this draw command
                    for (int i = 0; i < cmd.ElemCount; i += 3)
                    {
                        // Get triangle indices
                        ushort idx1 = idxBuffer[(int)cmd.IdxOffset + i];
                        ushort idx2 = idxBuffer[(int)cmd.IdxOffset + i + 1];
                        ushort idx3 = idxBuffer[(int)cmd.IdxOffset + i + 2];
                        
                        // Get vertices from buffer
                        ImDrawVertPtr v1 = vtxBuffer[idx1];
                        ImDrawVertPtr v2 = vtxBuffer[idx2];
                        ImDrawVertPtr v3 = vtxBuffer[idx3];

                        // Create SDL_Vertex array for this triangle
                        SDL.SDL_Vertex[] sdlVertices = new SDL.SDL_Vertex[3];
                        
                        // Set position, color and UV for first vertex
                        sdlVertices[0].position.x = v1.pos.X;
                        sdlVertices[0].position.y = v1.pos.Y;
                        sdlVertices[0].color.r = (byte)(v1.col & 0xFF);
                        sdlVertices[0].color.g = (byte)((v1.col >> 8) & 0xFF);
                        sdlVertices[0].color.b = (byte)((v1.col >> 16) & 0xFF);
                        sdlVertices[0].color.a = (byte)((v1.col >> 24) & 0xFF);
                        sdlVertices[0].tex_coord.x = v1.uv.X;
                        sdlVertices[0].tex_coord.y = v1.uv.Y;
                        
                        // Set position, color and UV for second vertex
                        sdlVertices[1].position.x = v2.pos.X;
                        sdlVertices[1].position.y = v2.pos.Y;
                        sdlVertices[1].color.r = (byte)(v2.col & 0xFF);
                        sdlVertices[1].color.g = (byte)((v2.col >> 8) & 0xFF);
                        sdlVertices[1].color.b = (byte)((v2.col >> 16) & 0xFF);
                        sdlVertices[1].color.a = (byte)((v2.col >> 24) & 0xFF);
                        sdlVertices[1].tex_coord.x = v2.uv.X;
                        sdlVertices[1].tex_coord.y = v2.uv.Y;
                        
                        // Set position, color and UV for third vertex
                        sdlVertices[2].position.x = v3.pos.X;
                        sdlVertices[2].position.y = v3.pos.Y;
                        sdlVertices[2].color.r = (byte)(v3.col & 0xFF);
                        sdlVertices[2].color.g = (byte)((v3.col >> 8) & 0xFF);
                        sdlVertices[2].color.b = (byte)((v3.col >> 16) & 0xFF);
                        sdlVertices[2].color.a = (byte)((v3.col >> 24) & 0xFF);
                        sdlVertices[2].tex_coord.x = v3.uv.X;
                        sdlVertices[2].tex_coord.y = v3.uv.Y;
                        
                        // Render this triangle with texture - corrected version
                        SDL.SDL_RenderGeometry(
                            _renderer,
                            _fontTexture,
                            sdlVertices,  // Pass the array directly
                            3,
                            null,  // No indices
                            0
                        );
                    }
                }
                else
                {
                    // Non-text triangles - use color fill approach
                    for (int i = 0; i < cmd.ElemCount; i += 3)
                    {
                        ushort idx1 = idxBuffer[(int)cmd.IdxOffset + i];
                        ushort idx2 = idxBuffer[(int)cmd.IdxOffset + i + 1];
                        ushort idx3 = idxBuffer[(int)cmd.IdxOffset + i + 2];
                        ImDrawVertPtr v1 = vtxBuffer[idx1];
                        ImDrawVertPtr v2 = vtxBuffer[idx2];
                        ImDrawVertPtr v3 = vtxBuffer[idx3];
                        
                        byte r1 = (byte)(v1.col & 0xFF);
                        byte g1 = (byte)((v1.col >> 8) & 0xFF);
                        byte b1 = (byte)((v1.col >> 16) & 0xFF);
                        byte a1 = (byte)((v1.col >> 24) & 0xFF);
                        
                        FillTriangle(_renderer,
                            new short[] { (short)v1.pos.X, (short)v2.pos.X, (short)v3.pos.X },
                            new short[] { (short)v1.pos.Y, (short)v2.pos.Y, (short)v3.pos.Y },
                            r1, g1, b1, a1);
                    }
                }
            }
        }
        
        // Reset clip rectangle
        SDL.SDL_RenderSetClipRect(_renderer, IntPtr.Zero);
    }

    // A simple triangle filling algorithm
    private static void FillTriangle(IntPtr renderer, short[] x, short[] y, byte r, byte g, byte b, byte a)
    {
        // Find the bounding box of the triangle
        short minX = Math.Min(Math.Min(x[0], x[1]), x[2]);
        short maxX = Math.Max(Math.Max(x[0], x[1]), x[2]);
        short minY = Math.Min(Math.Min(y[0], y[1]), y[2]);
        short maxY = Math.Max(Math.Max(y[0], y[1]), y[2]);
        
        // Set the color
        SDL.SDL_SetRenderDrawColor(renderer, r, g, b, a);
        
        // A very simple way to fill a triangle: just draw horizontal lines
        // For each row in the bounding box
        for (short py = minY; py <= maxY; py++)
        {
            // For each edge of the triangle, find where it intersects with this row
            List<float> intersections = new List<float>();
            
            // Check each edge
            for (int i = 0; i < 3; i++)
            {
                int j = (i + 1) % 3;
                
                // Skip horizontal edges
                if (y[i] == y[j])
                    continue;
                
                // If the edge crosses this row
                if ((y[i] <= py && py < y[j]) || (y[j] <= py && py < y[i]))
                {
                    // Find the x-coordinate of the intersection
                    float t = (py - y[i]) / (float)(y[j] - y[i]);
                    float px = x[i] + t * (x[j] - x[i]);
                    intersections.Add(px);
                }
            }
            
            // Sort the intersections from left to right
            intersections.Sort();
            
            // Draw lines between pairs of intersections
            for (int i = 0; i < intersections.Count - 1; i += 2)
            {
                int startX = (int)Math.Round(intersections[i]);
                int endX = (int)Math.Round(intersections[i + 1]);
                SDL.SDL_RenderDrawLine(renderer, startX, py, endX, py);
            }
        }
    }

    private static void InitializeAudio()
    {
        _synth = new MultipleWaveSynth();
        
        // add initial wave
        if (_waves.Count == 0)
        {
            _waves.Add(new WaveData(_frequency, _amplitude, (WaveType)_currentWaveType, new EnvelopeGenerator(), new Filter()));
        }

        UpdateSynthesizer();

        _waveOut = new WaveOutEvent();
        _waveOut.Init(_synth);
    }

    private static void RenderUI()
    {
        ImGui.SetNextWindowPos(new Vector2(0, 0));
        ImGui.SetNextWindowSize(new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT));
        ImGui.Begin("Synthesizer Controls", 
            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | 
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar);
        
        ImGui.Text("Waveform Visual");
        
        // Draw waveform visualization placeholder
        float width = ImGui.GetContentRegionAvail().X;
        float height = 100;
        Vector2 pos = ImGui.GetCursorScreenPos();
        
        ImDrawListPtr drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(pos, new Vector2(pos.X + width, pos.Y + height), 
            ImGui.ColorConvertFloat4ToU32(new Vector4(0.1f, 0.1f, 0.1f, 1.0f)));
            
        // Draw grid line
        float midY = pos.Y + height / 2;
        drawList.AddLine(
            new Vector2(pos.X, midY),
            new Vector2(pos.X + width, midY),
            ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.5f, 0.5f, 0.5f)));
            
        // Draw waveform points
        if (_waveFormPoints.Length > 0)
        {
            for (int i = 0; i < _waveFormPoints.Length - 1; i++)
            {
                float x1 = pos.X + (i * width / _waveFormPoints.Length);
                float y1 = midY - (_waveFormPoints[i] * height / 2);
                float x2 = pos.X + ((i + 1) * width / _waveFormPoints.Length);
                float y2 = midY - (_waveFormPoints[i + 1] * height / 2);
                
                drawList.AddLine(
                    new Vector2(x1, y1),
                    new Vector2(x2, y2),
                    ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 1.0f, 0.0f, 1.0f)),
                    2.0f);
            }
        }
        
        ImGui.Dummy(new Vector2(0, height));
        ImGui.Separator();
            
        if (_waves.Count == 0)
        {
            // Add initial wave if none exist
            _waves.Add(new WaveData(_frequency, _amplitude, (WaveType)_currentWaveType, new EnvelopeGenerator(), new Filter()));
        }
        
        bool wavesChanged = false;
        int waveToRemove = -1;
        
        // Display each wave's parameters
        for (int i = 0; i < _waves.Count; i++)
        {
            WaveData wave = _waves[i];
            
            ImGui.PushID(i);
            
            // Wave header with enable/disable checkbox
            bool isEnabled = wave.IsEnabled;
            if (ImGui.Checkbox($"Wave {i+1}##enabled{i}", ref isEnabled))
            {
                wave.IsEnabled = isEnabled;
                wavesChanged = true;
            }
            
            ImGui.SameLine();
            
            // Remove wave button
            if (ImGui.Button("X##remove" + i) && _waves.Count > 1)
            {
                waveToRemove = i;
                wavesChanged = true;
            }
            
            // Wave parameters
            float freq = wave.Frequency;
            if (ImGui.SliderFloat($"Frequency (Hz)##freq{i}", ref freq, 20.0f, 2000.0f))
            {
                wave.Frequency = freq;
                wavesChanged = true;
            }
            
            float amp = wave.Amplitude;
            if (ImGui.SliderFloat($"Amplitude##amp{i}", ref amp, 0f, 1f))
            {
                wave.Amplitude = amp;
                wavesChanged = true;
            }
            
            int waveType = (int)wave.WaveType;
            if (ImGui.Combo($"Wave Type##type{i}", ref waveType, _waveTypes, _waveTypes.Length))
            {
                wave.WaveType = (WaveType)waveType;
                wavesChanged = true;
            }
            
            ImGui.BulletText("ADSR (Envelope)");
            ImGui.SameLine();
            if (ImGui.Button("?##ADSRHelp", new Vector2(17, 17)))
            {
                _showAdsrHelp = !_showAdsrHelp;
                _selectedAdsrWaveIndex = i;
            }
            
            float attackTime = wave.Envelope.AttackTime;
            if (ImGui.SliderFloat($"Attack time##attackTime{i}", ref attackTime, 0f, 10f))
            {
                wave.Envelope.AttackTime = attackTime;
                wavesChanged = true;
            }
            
            float decayTime = wave.Envelope.DecayTime;
            if (ImGui.SliderFloat($"Decay time##decayTime{i}", ref decayTime, 0f, 10f))
            {
                wave.Envelope.DecayTime = decayTime;
                wavesChanged = true;
            }
            
            float sustainLevel = wave.Envelope.SustainLevel;
            if (ImGui.SliderFloat($"Sustain level##sustainLevel{i}", ref sustainLevel, 0f, 1f))
            {
                wave.Envelope.SustainLevel = sustainLevel;
                wavesChanged = true;
            }
            
            float releaseTime = wave.Envelope.ReleaseTime;
            if (ImGui.SliderFloat($"Release time##releaseTime{i}", ref releaseTime, 0f, 10f))
            {
                wave.Envelope.ReleaseTime = releaseTime;
                wavesChanged = true;
            }
            
            ImGui.BulletText("Filter");
            ImGui.SameLine();
            if (ImGui.Button("?##FilterHelp", new Vector2(17, 17)))
            {
                _showFilterHelp = !_showFilterHelp;
                _selectedFilterWaveIndex = i;
            }
            
            float cutoff = wave.Filter.Cutoff;
            if (ImGui.SliderFloat($"Cutoff##cutoff{i}", ref cutoff, 20f, 20000f))
            {
                wave.Filter.Cutoff = cutoff;
                wavesChanged = true;
            }
            
            float resonance = wave.Filter.Resonance;
            if (ImGui.SliderFloat($"Resonance##resonance{i}", ref resonance, 0.01f, 0.99f))
            {
                wave.Filter.Resonance = resonance;
                wavesChanged = true;
            }
            
            int filterType = (int)wave.Filter.Type;
            if (ImGui.Combo($"Type##filterType{i}", ref filterType, Filter.FilterTypes, Filter.FilterTypes.Length))
            {
                wave.Filter.Type = (FilterType) filterType;
                wavesChanged = true;
            }
            
            ImGui.Separator();
            ImGui.PopID();
            
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();
        }
        
        if (_showAdsrHelp)
        {
            ImGui.SetNextWindowPos(ImGui.GetMousePos(), ImGuiCond.Appearing);
            ImGui.SetNextWindowSize(new Vector2(600, 500));

            if (ImGui.Begin("ADSR Help", ref _showAdsrHelp,
                    ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize))
            {
                RenderAdsrHelpContent();
            }
        }
        
        if (_showFilterHelp)
        {
            ImGui.SetNextWindowPos(ImGui.GetMousePos(), ImGuiCond.Appearing);
            ImGui.SetNextWindowSize(new Vector2(600, 500));

            if (ImGui.Begin("Filter Help", ref _showFilterHelp,
                    ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize))
            {
                RenderFilterHelpContent();
            }
        }
        
        // Remove wave if requested
        if (waveToRemove >= 0 && _waves.Count > 1)
        {
            _waves.RemoveAt(waveToRemove);
        }
        
        // Add wave and play buttons in a row
        ImGui.BeginGroup();
        
        if (ImGui.Button(_isPlaying ? "Note Off" : "Note On", new Vector2(70, 20)))
        {
            if (!_isPlaying)
            {
                // Start playing and trigger Note On
                _isPlaying = true;
                _waveOut.Play();
                _synth.NoteOn();
            }
            else
            {
                // Trigger Note Off but keep audio device running
                _isPlaying = false;
                _synth.NoteOff();
            }
        }
        
        ImGui.SameLine();
        
        if (ImGui.Button("Add wave", new Vector2(70, 20)))
        {
            // Clone the last wave's settings for the new wave
            _waves.Add(_waves[^1].Clone());
            wavesChanged = true;
        }
        
        ImGui.EndGroup();

        bool useAntiAliasing = _synth.UseAntiAliasing;
        if (ImGui.Checkbox("Use anti-aliasing", ref useAntiAliasing))
        {
            _synth.UseAntiAliasing = useAntiAliasing;
        }
        
        // Update synth if any parameters changed
        if (wavesChanged)
        {
            UpdateSynthesizer();
        }
        
        ImGui.End();
    }

    private static void RenderAdsrHelpContent()
    {
        ImGui.Text($"ADSR Help (selected wave index: {_selectedAdsrWaveIndex})");
        ImGui.Separator();

        if (ImGui.CollapsingHeader("ADSR"))
        {
            ImGui.TextWrapped("ADSR - Attack, Decay, Sustain, Release");
            ImGui.Bullet(); ImGui.TextWrapped("Attack -> How quickly sound rises from 0 to max volume");
            ImGui.Bullet(); ImGui.TextWrapped("Decay -> How quickly sound falls from max volume to sustain level");
            ImGui.Bullet(); ImGui.TextWrapped("Sustain -> Volume level maintained while note is held");
            ImGui.Bullet(); ImGui.TextWrapped("Release -> Fade out time");
        }

        if (ImGui.CollapsingHeader("Sound presets"))
        {
            if (ImGui.BeginTable("presetTable", 5, ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn("Preset");
                ImGui.TableSetupColumn("Attack");
                ImGui.TableSetupColumn("Decay");
                ImGui.TableSetupColumn("Sustain");
                ImGui.TableSetupColumn("Release");
                ImGui.TableHeadersRow();
                
                RenderAdsrPresetRow("Plucked string", 0.005f, 0.1f, 0.0f, 0.1f, _selectedAdsrWaveIndex);
                RenderAdsrPresetRow("Piano", 0.01f, 0.2f, 0.5f, 0.5f, _selectedAdsrWaveIndex);
                RenderAdsrPresetRow("Organ", 0.02f, 0.0f, 1.0f, 0.05f, _selectedAdsrWaveIndex);
                RenderAdsrPresetRow("Pad/Strings", 0.5f, 0.5f, 0.8f, 1f, _selectedAdsrWaveIndex);
                RenderAdsrPresetRow("Brass", 0.1f, 0.1f, 0.8f, 0.2f, _selectedAdsrWaveIndex);
                RenderAdsrPresetRow("Percussive", 0.001f, 0.2f, 0f, 0.01f, _selectedAdsrWaveIndex);
                
                ImGui.EndTable();
            }
        }
        
        if (ImGui.CollapsingHeader("Mood presets"))
        {
            if (ImGui.BeginTable("moodTable", 5, ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn("Mood");
                ImGui.TableSetupColumn("Attack");
                ImGui.TableSetupColumn("Decay");
                ImGui.TableSetupColumn("Sustain");
                ImGui.TableSetupColumn("Release");
                ImGui.TableHeadersRow();
                
                RenderAdsrPresetRow("Happy/Upbeat", 0.01f, 0.1f, 0.7f, 0.2f, _selectedAdsrWaveIndex);
                RenderAdsrPresetRow("Sad", 0.15f, 0.4f, 0.5f, 1f, _selectedAdsrWaveIndex);
                RenderAdsrPresetRow("Tense", 0.005f, 0.05f, 0.9f, 0.1f, _selectedAdsrWaveIndex);
                RenderAdsrPresetRow("Mysterious", 0.3f, 0.5f, 0.6f, 0.8f, _selectedAdsrWaveIndex);
                RenderAdsrPresetRow("Aggressive", 0.001f, 0.1f, 0.8f, 0.05f, _selectedAdsrWaveIndex);
                RenderAdsrPresetRow("Ambient", 0.8f, 1f, 0.7f, 2f, _selectedAdsrWaveIndex);
                
                ImGui.EndTable();
            }
        }
        
        if (ImGui.Button("Close", new Vector2(70, 20)))
        {
            _showAdsrHelp = false;
        }
                    
        ImGui.End();
    }

    private static void RenderAdsrPresetRow(string name, float attack, float decay, float sustain, float release, int waveIndex)
    {
        ImGui.TableNextRow();
        
        ImGui.TableNextColumn();
        if (ImGui.Button($"Apply##{name}"))
        {
            _waves[waveIndex].Envelope.AttackTime = attack;
            _waves[waveIndex].Envelope.DecayTime = decay;
            _waves[waveIndex].Envelope.SustainLevel = sustain;
            _waves[waveIndex].Envelope.ReleaseTime = release;
            UpdateSynthesizer();
        }
        ImGui.SameLine();
        ImGui.Text(name);
        
        ImGui.TableNextColumn();
        ImGui.Text($"{attack:F3}");
        ImGui.TableNextColumn();
        ImGui.Text($"{decay:F3}");
        ImGui.TableNextColumn();
        ImGui.Text($"{sustain:F3}");
        ImGui.TableNextColumn();
        ImGui.Text($"{release:F3}");
    }
    
    private static void RenderFilterHelpContent()
    {
        ImGui.Text($"Filter Help (selected wave index: {_selectedFilterWaveIndex})");
        ImGui.Separator();

        if (ImGui.CollapsingHeader("Cutoff"))
        {
            ImGui.Bullet(); ImGui.TextWrapped("LowPass: only frequencies below Cutoff pass through");
            ImGui.Bullet(); ImGui.TextWrapped("HighPass: only frequencies above Cutoff pass through");
            ImGui.Bullet(); ImGui.TextWrapped("BandPass: only frequencies near Cutoff pass through");
            ImGui.Bullet(); ImGui.TextWrapped("Notch: frequencies near Cutoff are removed");
        }

        if (ImGui.CollapsingHeader("Sound presets"))
        {
            if (ImGui.BeginTable("filterPresetTable", 4, ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn("Preset");
                ImGui.TableSetupColumn("Cutoff");
                ImGui.TableSetupColumn("Resonance");
                ImGui.TableSetupColumn("Type");
                ImGui.TableHeadersRow();
                
                RenderFilterPresetRow("Plucked string", 3000, 0.3f, FilterType.LowPass, _selectedFilterWaveIndex);
                RenderFilterPresetRow("Piano", 5000, 0.1f, FilterType.LowPass, _selectedFilterWaveIndex);
                RenderFilterPresetRow("Organ", 2500, 0.6f, FilterType.LowPass, _selectedFilterWaveIndex);
                RenderFilterPresetRow("Pad/Strings", 2000, 0.2f, FilterType.LowPass, _selectedFilterWaveIndex);
                RenderFilterPresetRow("Brass", 1500, 0.7f, FilterType.LowPass, _selectedFilterWaveIndex);
                RenderFilterPresetRow("Percussive", 1000, 0.5f, FilterType.BandPass, _selectedFilterWaveIndex);
                
                ImGui.EndTable();
            }
        }
        
        if (ImGui.CollapsingHeader("Mood presets"))
        {
            if (ImGui.BeginTable("filterMoodTable", 4, ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn("Mood");
                ImGui.TableSetupColumn("Cutoff");
                ImGui.TableSetupColumn("Resonance");
                ImGui.TableSetupColumn("Type");
                ImGui.TableHeadersRow();
                
                RenderFilterPresetRow("Happy/Upbeat", 5000, 0.2f, FilterType.LowPass, _selectedFilterWaveIndex);
                RenderFilterPresetRow("Sad", 1000, 0.1f, FilterType.LowPass, _selectedFilterWaveIndex);
                RenderFilterPresetRow("Tense", 500, 0.8f, FilterType.HighPass, _selectedFilterWaveIndex);
                RenderFilterPresetRow("Mysterious", 800, 0.6f, FilterType.BandPass, _selectedFilterWaveIndex);
                RenderFilterPresetRow("Aggressive", 3000, 0.9f, FilterType.LowPass, _selectedFilterWaveIndex);
                RenderFilterPresetRow("Ambient", 1200, 0.3f, FilterType.LowPass, _selectedFilterWaveIndex);
                
                ImGui.EndTable();
            }
        }
        
        if (ImGui.Button("Close", new Vector2(70, 20)))
        {
            _showFilterHelp = false;
        }
                    
        ImGui.End();
    }

    private static void RenderFilterPresetRow(string name, float cutoff, float resonance, FilterType type, int waveIndex)
    {
        ImGui.TableNextRow();
        
        ImGui.TableNextColumn();
        if (ImGui.Button($"Apply##{name}"))
        {
            _waves[waveIndex].Filter.Cutoff = cutoff;
            _waves[waveIndex].Filter.Resonance = resonance;
            _waves[waveIndex].Filter.Type = type;
            UpdateSynthesizer();
        }
        ImGui.SameLine();
        ImGui.Text(name);
        
        ImGui.TableNextColumn();
        ImGui.Text($"{cutoff:F3}");
        ImGui.TableNextColumn();
        ImGui.Text($"{resonance:F3}");
        ImGui.TableNextColumn();
        ImGui.Text($"{type}");
    }
    
    private static void UpdateSynthesizer()
    {
        while (_synth.Waves.Count < _waves.Count)
        {
            // Add any missing waves
            int index = _synth.Waves.Count;
            var wave = _waves[index];
            _synth.AddWave(wave.Frequency, wave.Amplitude, wave.WaveType, wave.IsEnabled);
        }
    
        while (_synth.Waves.Count > _waves.Count)
        {
            // Remove any extra waves
            _synth.RemoveWave(_synth.Waves.Count - 1);
        }
        
        for (int i = 0; i < _waves.Count; i++)
        {
            var uiWave = _waves[i];
            _synth.UpdateWave(i, uiWave.Frequency, uiWave.Amplitude, uiWave.WaveType, uiWave.IsEnabled, uiWave.Envelope, uiWave.Filter);
        }
    }

    private static void UpdateWaveformVisuals()
    {
        // Clear out points if no waves are enabled
        if (!_waves.Any(w => w.IsEnabled))
        {
            Array.Clear(_waveFormPoints, 0, _waveFormPoints.Length);
            return;
        }
        
        // Calculate combined waveform
        for (int i = 0; i < _waveFormPoints.Length; i++)
        {
            // Convert array index to a time position
            double timePosition = (i / (double)_waveFormPoints.Length) * _timeWindow;
            float sample = 0f;
            
            // Sum all enabled waves
            foreach (var wave in _waves.Where(w => w.IsEnabled))
            {
                // Calculate phase based on frequency and time
                double phase = 2 * Math.PI * wave.Frequency * timePosition;
                
                sample += _synth.GenerateWaveSample(phase, wave.Frequency, wave.Amplitude, wave.WaveType);
            }
            
            _waveFormPoints[i] = sample;
        }
    }
}