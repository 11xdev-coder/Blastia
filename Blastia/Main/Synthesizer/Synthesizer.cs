using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SDL2;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace Blastia.Main.Synthesizer;

public class Synthesizer
{
    private static IntPtr _window;
    private static IntPtr _renderer;
    private static IntPtr _imGuiContext;
    
    // synth
    private static float _frequency = 444.0f;
    private static float _amplitude = 0.5f;
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
    private static WaveSynthesizer _synth;
    private static WaveOutEvent _waveOut;
    private static uint _time;
    private static IntPtr _fontTexture;

    public static void Launch(string[] args)
    {
        if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) < 0)
        {
            Console.WriteLine($"Error: SDL Initialization Error: {SDL.SDL_GetError()}");
            return;
        }
        
        // create window
        _window = SDL.SDL_CreateWindow("Blastia Synthesizer", SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED,  800, 600, SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);
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
        string path = @"D:\Projects\Blastia\Blastia\Main\Content\Font\Roboto_Condensed-Thin.ttf";
        float fontSize = 14.0f;
        ImFontConfigPtr config = new ImFontConfigPtr();
        io.Fonts.AddFontFromFileTTF(path, fontSize, config, io.Fonts.GetGlyphRangesDefault());
        
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
        _synth = new WaveSynthesizer((WaveSynthesizer.WaveType)_currentWaveType);
        _synth.SetFrequency(_frequency);
        _synth.SetAmplitude(_amplitude);

        _waveOut = new WaveOutEvent();
        _waveOut.Init(_synth);
    }

    private static void RenderUI()
    {
        ImGui.SetNextWindowPos(new Vector2(0, 0));
        ImGui.SetNextWindowSize(new Vector2(800, 600));
        ImGui.Begin("Synthesizer Controls", 
            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | 
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar);
        
        ImGui.Text("Waveform Visual");
        
        // Draw waveform visualization placeholder
        float width = ImGui.GetContentRegionAvail().X;
        float height = 200;
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
        
        // Controls
        if (ImGui.SliderFloat("Frequency (Hz)", ref _frequency, 20.0f, 2000.0f))
        {
            _synth.SetFrequency(_frequency);
        }
        
        if (ImGui.SliderFloat("Amplitude", ref _amplitude, 0f, 1f))
        {
            _synth.SetAmplitude(_amplitude);
        }
        
        if (ImGui.Combo("Wave Type", ref _currentWaveType, _waveTypes, _waveTypes.Length))
        {
            _synth.SetWaveType((WaveSynthesizer.WaveType)_currentWaveType);
        }
        
        if (ImGui.Button(_isPlaying ? "Stop" : "Play", new Vector2(100, 40)))
        {
            _isPlaying = !_isPlaying;
            
            if (_isPlaying)
            {
                _waveOut.Play();
            }
            else
            {
                _waveOut.Stop();
            }
        }
        
        ImGui.End();
    }

    private static void UpdateWaveformVisuals()
    {
        // Generate waveform points for visualization
        double samplesPerCycle = _waveFormPoints.Length / 2.0;
        
        for (int i = 0; i < _waveFormPoints.Length; i++)
        {
            double phase = 2 * Math.PI * i / samplesPerCycle;
            _waveFormPoints[i] = (float)(_synth.GenerateSample(phase) * _amplitude);
        }
    }

}