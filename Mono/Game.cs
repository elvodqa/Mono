using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using Mono.Audio;
using Mono.Graphics;
using Mono.Storage;
using Mono.Timing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System.Diagnostics;
using SDL;
using static SDL.SDL3;
using Console = Mono.Debug.Console;

namespace Mono;

public class BindingContext : IBindingsContext
{
    public IntPtr GetProcAddress(string procName)
    {
        return SDL_GL_GetProcAddress(procName);
    }
}

public class TrackedVariable
{
    public string Name { get; init; }
    public Func<object?> Getter { get; init; }
}

public unsafe class Game : IDisposable
{
    private string Title;

    public Window Window { get; private set; }
    private SDL_GLContextState* _glContextState;

    private bool _running;
    private bool _debugInfo = true;
    private FramedClock _framedClock;
    
    private Console _console;
    private Texture RectTexture;

    public List<TrackedVariable> TrackedVariables { get; } = new();

    public TitleStorage RootTitleStorage { get; internal set; }
    public SpriteBatch SpriteBatch { get; internal set; }
    public GraphicsDevice GraphicsDevice { get; internal set; }
    public Font DefaultFont;

    public Game()
    {
        Title = "Title";
    }
    
    private void Initialize()
    {
#if HIGH_PRECISION_AUDIO
        SDL_SetHint(SDL_HINT_AUDIO_DEVICE_SAMPLE_FRAMES, "128");
#endif
        if (!SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO | SDL_InitFlags.SDL_INIT_AUDIO))
        {
            throw new Exception(SDL_GetError());
        }

        if (!SDL3_mixer.MIX_Init())
        {
            throw new Exception(SDL_GetError());
        }

        SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_MAJOR_VERSION, 4);
        SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_CONTEXT_MINOR_VERSION, 1);
        SDL_GL_SetAttribute(
            SDL_GLAttr.SDL_GL_CONTEXT_FLAGS,
            SDL_GL_CONTEXT_FORWARD_COMPATIBLE_FLAG |
            SDL_GL_CONTEXT_DEBUG_FLAG);
        SDL_GL_SetAttribute(
            SDL_GLAttr.SDL_GL_CONTEXT_PROFILE_MASK,
            SDL_GL_CONTEXT_PROFILE_CORE);
        SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_DOUBLEBUFFER, 1);
        SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_DEPTH_SIZE, 24);

        Window = new Window("Title", new Vector2i(1280, 720));
        _glContextState = SDL_GL_CreateContext(Window.Handle);
        SDL_GL_MakeCurrent(Window.Handle, _glContextState);
        GLLoader.LoadBindings(new BindingContext());

        int interval;
        if (!SDL_GL_SetSwapInterval(-1))
        {
            SDL_GL_SetSwapInterval(0);
        }

        SDL_GL_GetSwapInterval(&interval);

        Logger.Info($"BasePath: {SDL_GetBasePath()}");
        Logger.Info($"Vendor:   {GL.GetString(StringName.Vendor)}");
        Logger.Info($"Renderer: {GL.GetString(StringName.Renderer)}");
        Logger.Info($"Version:  {GL.GetString(StringName.Version)}");
        Logger.Info($"GLSL:     {GL.GetString(StringName.ShadingLanguageVersion)}");
        Logger.Info($"Swap:     {interval}");

        
        _framedClock = new FramedClock(new StopwatchClock(true));
        _running = false;


        RootTitleStorage = new TitleStorage();
        
        GraphicsDevice = new GraphicsDevice(Window);
        SpriteBatch = new SpriteBatch(GraphicsDevice, RootTitleStorage);
        DefaultFont = Font.Load(RootTitleStorage, "Assets/Fonts/roboto/roboto.png", "Assets/Fonts/roboto/roboto.json");
        RectTexture = new Texture(RootTitleStorage, "Assets/Textures/rect.png");

        

        _console = new Console();
        _console.Init(GraphicsDevice, RootTitleStorage);
        
        Logger.SetConsole(_console);


        _console.RegisterCommand("play", args =>
        {
            if (args.Count < 1)
            {
                _console.AddConsoleItem("Need param: song_name");
                return;
            }

            var audio = AudioManager.LoadAudio(RootTitleStorage, $"Assets/Audio/{args[0]}.mp3");
            AudioManager.PlaySample(audio);
        });

        _console.RegisterCommand("samplelist", args =>
        {
            var tracks = AudioManager.SampleTracks;
            if (tracks.Count == 0)
            {
                _console.AddConsoleItem("No active sample track.");
                return;
            }

            int i = 0;
            foreach (var sampleTrack in tracks)
            {
                _console.AddConsoleItem(
                    $"[{i++}] Track with audio({sampleTrack.Audio!.Path}): {sampleTrack.Position}/{sampleTrack.Length}");
            }
        });
        
        _console.RegisterCommand("sample_volume", args =>
        {
            if (args.Count != 1) return;
            float.TryParse(args[0], out var value);
            value = Math.Clamp(value, 0.0f, 1.0f);
            AudioManager.SampleVolume = value;
        });
        
        _console.RegisterCommand("sample_speed", args =>
        {
            if (args.Count != 1) return;
            float.TryParse(args[0], out var value);
            value = Math.Clamp(value, 0.0f, 10.0f);
            AudioManager.SampleSpeed = value;
        });
    }

    public virtual void Load()
    {
    }

    public virtual void Update(double elapsedFrameTime)
    {
    }

    public virtual void Draw(double elapsedFrameTime)
    {
    }


    public void Run()
    {
        Initialize();
        Load();

        _running = true;
        
        Input.GetKeyboardState();
        Input.UpdateMouse(Window.Handle);

        while (_running)
        {
            Input.FixScrollback();
            SDL_PumpEvents();
            
            SDL_Event ev;
            while (SDL_PollEvent(&ev))
            {
                switch (ev.type)
                {
                    case (uint)SDL_EventType.SDL_EVENT_QUIT:
                        _running = false;
                        break;
                    case (uint)SDL_EventType.SDL_EVENT_WINDOW_FOCUS_GAINED:
                        if (Window.CursorVisible) SDL_ShowCursor();
                        else SDL_HideCursor();
                        break;
                }

                Input.UpdateEvents(ev);
            }

            Input.GetKeyboardState();
            Input.UpdateMouse(Window.Handle);

            var updateBegin = Stopwatch.GetTimestamp();
            AudioManager.Update(_framedClock.ElapsedFrameTime);
            _console.Update(_framedClock.ElapsedFrameTime, Window.Handle);
            TweenSystem.Update(_framedClock.ElapsedFrameTime);
            if (Input.IsKeyPressed(SDL_Scancode.SDL_SCANCODE_F12)) _debugInfo = !_debugInfo;

            if (Window.CursorVisible) SDL_ShowCursor();
            else SDL_HideCursor();
            
            Update(_framedClock.ElapsedFrameTime);
            var updateEnd =  Stopwatch.GetTimestamp();
            
            var drawBegin =   Stopwatch.GetTimestamp();
            Draw(_framedClock.ElapsedFrameTime);
            
            // Draw console
            GraphicsDevice.RenderTarget = null;

            GraphicsDevice.SetViewport(new Rectangle(0, 0, (int)GraphicsDevice.FramebufferSize.X, (int)GraphicsDevice.FramebufferSize.Y));
            var drawEnd =  Stopwatch.GetTimestamp();

            var consoleDrawBegin = Stopwatch.GetTimestamp();
            _console.Draw(SpriteBatch);
            var consoleDrawEnd = Stopwatch.GetTimestamp();
            
            var updateTime = (updateEnd - updateBegin) * 1000f / Stopwatch.Frequency;
            var drawTime = (drawEnd - drawBegin) * 1000f / Stopwatch.Frequency;
            var consoleDrawTime = (consoleDrawEnd - consoleDrawBegin) * 1000f / Stopwatch.Frequency;
            
            if (_debugInfo)
            {
                var process = Process.GetCurrentProcess();

                long ram = process.WorkingSet64;
                
                var lineHeight = GraphicsDevice.FramebufferSize.Y / 60;
                var text = $"FPS: {_framedClock.FramesPerSecond} [{_framedClock.AverageFrameTime:F2} ms]\n" +
                           $"Batch Count: {SpriteBatch.BatchCount} | Draw Calls: {SpriteBatch.DrawCallCount}\n" + 
                           $"Console: {consoleDrawTime:F2} ms | Enabled: {_console.consoleEnabled}\n" +
                           $"Update: {updateTime:F2} ms | Draw: {drawTime:F2} ms\n" +
                           $"RenderTarget: {GraphicsDevice.FramebufferSize.X}x{GraphicsDevice.FramebufferSize.Y}\n" +
                           $"{GL.GetString(StringName.Version)}\n" +
                           $"{GL.GetString(StringName.Renderer)}\n" +
                           $"RAM: {SDL_GetSystemRAM()} MB\n" + 
                           $"RAM used: {ram / 1024 / 1024} MB";
                if (TrackedVariables.Count != 0) text += $"\n\nTracked ({TrackedVariables.Count}):\n";
                for (int i = 0; i < TrackedVariables.Count; i++)
                {
                    var variable = TrackedVariables[i];
                    text += $"  -   {variable.Name}: {variable.Getter()}";
                    if (i != TrackedVariables.Count - 1) text += "\n";
                }
                
                SpriteBatch.Begin(SpriteBatch.MakeDefault2DProjection(), Matrix4.Identity);
                var measure = Font.MeasureText(DefaultFont, text, lineHeight);
                SpriteBatch.Draw(RectTexture, new Rectangle(10, 10, (int)measure.Width, (int)measure.Height),
                    Color4.Black with { W = 0.8f }, 0, Vector2.Zero);
                SpriteBatch.DrawText(DefaultFont, text,
                    new Vector2(10, 10), lineHeight, Color4.White);
                
                //SpriteBatch.DrawText(DefaultFont, "Hello World!!, foo",
                //    new Vector2(10, 10), 200, Color4.White);
                SpriteBatch.End();
            }

            SpriteBatch.DrawCallCount = 0;
            SpriteBatch.BatchCount = 0;
            
            SDL_GL_SwapWindow(Window.Handle);
            _framedClock.ProcessFrame();
        }
    }
    
    public void TrackVariable(string name, Func<object?> getter)
    {
        TrackedVariables.Add(new TrackedVariable
        {
            Name = name,
            Getter = getter
        });
    }

    public void RegisterCommand(string name,
        Action<List<string>> callback)
    {
        _console.RegisterCommand(name, callback);
    }
    
    public void Dispose()
    {
        SDL_GL_DestroyContext(_glContextState);
        Window.Dispose();
    }
}