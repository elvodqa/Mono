using System.Drawing;
using System.Numerics;
using Mono.Graphics;
using Mono.Storage;
using OpenTK.Mathematics;
using SDL;
using Vector2 = OpenTK.Mathematics.Vector2;
using Vector4 = OpenTK.Mathematics.Vector4;

namespace Mono.Debug;

public class Console
{
    private static readonly Color4<Rgba> C_BG_DARK =
        new(14 / 255f, 61 / 255f, 43 / 255f, 1);

    private static readonly Color4<Rgba> C_BG =
        new(19 / 255f, 71 / 255f, 51 / 255f, 1);

    private static readonly Color4<Rgba> C_ACTIVE =
        new(20 / 255f, 80 / 255f, 60 / 255f, 1);

    private static readonly Color4<Rgba> C_LIGHT =
        new(45 / 255f, 120 / 255f, 90 / 255f, 1);

    private static readonly Color4<Rgba> C_SCROLL_DRAG =
        new(100 / 255f, 100 / 255f, 100 / 255f, 1);

    private static readonly Color4<Rgba> C_SCROLL_NORMAL =
        new(80 / 255f, 80 / 255f, 80 / 255f, 1);

    private static readonly Color4<Rgba> C_SCROLL_BG =
        new(40 / 255f, 40 / 255f, 40 / 255f, 1);

    private readonly List<string> consoleItems = new();

    private readonly Dictionary<string, Action<List<string>>> commands = new();


    internal bool consoleEnabled = false;
    private int consoleTween = -1;
    private float consoleHeight = 0;
    private int consoleWidth = 0;
    private float maxConsoleHeight = 1000;
    private float textHeight;
    private float cursorAlpha = 1;
    private int cursorAlphaTween = -1;
    private string inputBuffer = "";
    private bool showingAutofill = false;
    private int selectedAutofill = 0;
    private string selectedAutofillStr = "";
    private int autofillCount = 0;
   


    private Texture RectTexture;
    private Font DefaultFont;

    private GraphicsDevice graphicsDevice;

    private List<string> history = new();

    public void Init(GraphicsDevice graphicsDevice, TitleStorage storage)
    {
        this.graphicsDevice = graphicsDevice;
        
        RegisterCommand("exit", args => { Environment.Exit(0); });


        RegisterCommand("clear", args => { consoleItems.Clear(); });


        RegisterCommand("echo", args =>
        {
            foreach (var a in args)
                AddConsoleItem(a);
        });
        
        RegisterCommand("long_string", args =>
        {
            var str = "test";
            for (int i = 0; i < 400; i++)
            {
                str += " test";
            }
            AddConsoleItem(str);
        });

        RegisterCommand("log", args =>
        {
            if (args.Count > 0)
            {
                Logger.Trace(string.Join(" ", args));
            }
        });


        RegisterCommand("help", args =>
        {
            string allCommands = "";

            foreach (var key in commands.Keys)
            {
                allCommands += key + ", ";
            }

            AddConsoleItem(allCommands);
        });

        RectTexture = new Texture(storage, "Assets/Textures/rect.png");
        DefaultFont = Font.Load(storage, "Assets/Fonts/tahoma/tahoma.png", "Assets/Fonts/tahoma/tahoma.json");
    }

    public unsafe void Update(double dt, SDL_Window* window)
    {
        Input.Enabled = true;
        
        if (Input.IsKeyPressed(SDL_Scancode.SDL_SCANCODE_GRAVE) &&
            !consoleEnabled &&
            !TweenSystem.IsRunning(consoleTween))
        {
            consoleEnabled = true;


            consoleTween = TweenSystem.Tween(
                val => consoleHeight = val,
                0,
                maxConsoleHeight,
                0.1f,
                new TweenOptions()
                {
                    Func = EasingFunctions.OutExpo
                }
            );

            Input.BeginTextInput(window);

            cursorAlphaTween = TweenSystem.Tween(
                val => cursorAlpha = val,
                1,
                0,
                0.5f,
                new TweenOptions
                {
                    Yoyo = true,
                    Loops = -1
                });
        }


        if (Input.IsKeyPressed(SDL_Scancode.SDL_SCANCODE_ESCAPE) &&
            consoleEnabled &&
            !TweenSystem.IsRunning(consoleTween))
        {
            consoleEnabled = false;


            consoleTween = TweenSystem.Tween(
                val => consoleHeight = val,
                consoleHeight,
                0,
                0.1f,
                new TweenOptions()
                {
                    Func = EasingFunctions.OutExpo
                }
            );


            Input.EndTextInput(window);


            TweenSystem.Stop(cursorAlphaTween);


            showingAutofill = false;
            selectedAutofill = 0;
            selectedAutofillStr = "";
            inputBuffer = "";
        }


        if (consoleEnabled)
        {
            inputBuffer += Input.TextInput;


            if (Input.IsKeyPressed(SDL_Scancode.SDL_SCANCODE_TAB))
            {
                if (!showingAutofill)
                {
                    showingAutofill = true;
                }
                else if (!string.IsNullOrEmpty(selectedAutofillStr))
                {
                    inputBuffer = selectedAutofillStr + " ";
                    showingAutofill = false;
                    selectedAutofill = 0;
                }
            }
            
            
            if (Input.IsKeyPressed(SDL_Scancode.SDL_SCANCODE_BACKSPACE))
            {
                if (inputBuffer.Length > 0)
                    inputBuffer =
                        inputBuffer.Substring(0, inputBuffer.Length - 1);
            }


            if (Input.IsKeyPressed(SDL_Scancode.SDL_SCANCODE_RETURN))
            {
                if (showingAutofill &&
                    !string.IsNullOrEmpty(selectedAutofillStr))
                {
                    inputBuffer = selectedAutofillStr + " ";
                    showingAutofill = false;
                    selectedAutofill = 0;
                    return;
                }


                consoleItems.Add("> " + inputBuffer);

                ExecuteCommand(inputBuffer.Trim());

                history.Add(inputBuffer);
                
                inputBuffer = "";
            }


            if (showingAutofill)
            {
                if (Input.IsKeyPressed(SDL_Scancode.SDL_SCANCODE_UP))
                    selectedAutofill--;


                if (Input.IsKeyPressed(SDL_Scancode.SDL_SCANCODE_DOWN))
                    selectedAutofill++;


                if (autofillCount > 0)
                {
                    selectedAutofill =
                        Math.Clamp(
                            selectedAutofill,
                            0,
                            autofillCount - 1);
                }
            }
            else
            {
                if (Input.IsKeyPressed(SDL_Scancode.SDL_SCANCODE_UP))
                    if (history.Count > 0)
                        inputBuffer = history.Last();
            }
        }


        if (consoleEnabled)
            Input.Enabled = false;
        else
            Input.Enabled = true;
    }


    public void Draw(SpriteBatch spriteBatch)
    {
        consoleWidth = graphicsDevice.FramebufferSize.X;
        maxConsoleHeight = (int)(graphicsDevice.FramebufferSize.Y / 2f);
        textHeight = graphicsDevice.FramebufferSize.Y / 50f;

        if (consoleHeight == 0)
            return;


        spriteBatch.Begin(
            spriteBatch.MakeDefault2DProjection(),
            Matrix4.Identity
        );


        var bg = C_BG;
        bg.W = 0.8f;


        spriteBatch.Draw(
            RectTexture,
            new Rectangle(
                0,
                0,
                (int)consoleWidth,
                (int)consoleHeight),
            bg, 0, Vector2.Zero);


        float inputAreaHeight = textHeight + 5;

        // input area

        var inputAreaRect = new Rectangle(
            0,
            (int)(consoleHeight - inputAreaHeight),
            (int)consoleWidth,
            (int)inputAreaHeight);
        
        //spriteBatch.Flush();
        //spriteBatch.PushScissor(inputAreaRect);
        
        spriteBatch.Draw(
            RectTexture,
            inputAreaRect,
            C_ACTIVE, 0, Vector2.Zero);
        float padding = 20;
        
        spriteBatch.DrawText(
            DefaultFont,
            inputBuffer,
            new Vector2(
                padding,
                consoleHeight - inputAreaHeight + 2),
            textHeight,
            Color4.White);

        float cursorX =
            padding +
            Font.MeasureText(
                DefaultFont,
                inputBuffer,
                textHeight).Width;
        
        
        spriteBatch.Draw(
            RectTexture,
            new Rectangle(
                (int)cursorX,
                (int)(consoleHeight - inputAreaHeight +
                      inputAreaHeight / 2),
                5,
                (int)(inputAreaHeight * 0.9f)),
            new Color4<Rgba>(1, 1, 1, cursorAlpha), 0, new Vector2(0.5f, 0.5f));
        
        //spriteBatch.Flush();
        //spriteBatch.PopScissor();
        
        float y = consoleHeight - inputAreaHeight - 5;
        
        foreach (var item in consoleItems.AsEnumerable().Reverse())
        {
            float h =
                Font.MeasureText(
                        DefaultFont,
                        item,
                        textHeight,
                        consoleWidth - padding * 2)
                    .Height;
            
            if (y - h < 0)
                break;
            
            y -= h;
            
            spriteBatch.DrawText(
                DefaultFont,
                item,
                new Vector2(padding, y),
                textHeight,
                Color4.White, consoleWidth - padding * 2);
        }

        if (showingAutofill)
        {
            List<string> matches = new();


            foreach (var key in commands.Keys)
            {
                if (key.StartsWith(inputBuffer.Trim()))
                    matches.Add(key);
            }


            autofillCount =
                Math.Min(matches.Count, 6);


            if (autofillCount > 0)
            {
                selectedAutofillStr =
                    matches[selectedAutofill];


                float maxWidth = 0;


                for (int i = 0; i < autofillCount; i++)
                {
                    maxWidth =
                        Math.Max(
                            maxWidth,
                            Font.MeasureText(
                                    DefaultFont,
                                    matches[i],
                                    textHeight)
                                .Width);
                }


                maxWidth += padding * 2;


                cursorX =
                    padding +
                    Font.MeasureText(
                        DefaultFont,
                        inputBuffer,
                        textHeight).Width;


                spriteBatch.Draw(
                    RectTexture,
                    new Rectangle(
                        (int)cursorX,
                        (int)consoleHeight,
                        (int)maxWidth,
                        (int)(inputAreaHeight * autofillCount)),
                    C_BG, 0, Vector2.Zero);


                for (int i = 0; i < autofillCount; i++)
                {
                    if (i == selectedAutofill)
                    {
                        spriteBatch.Draw(
                            RectTexture,
                            new Rectangle(
                                (int)cursorX,
                                (int)(consoleHeight +
                                      i * inputAreaHeight),
                                (int)maxWidth,
                                (int)inputAreaHeight),
                            C_LIGHT, 0, Vector2.Zero);
                    }
                    spriteBatch.DrawText(
                        DefaultFont,
                        matches[i],
                        new Vector2(
                            cursorX + padding,
                            consoleHeight +
                            i * inputAreaHeight + 2),
                        textHeight,
                        Color4.White);
                }
            }
            else
            {
                selectedAutofillStr = "";
            }
        }


        spriteBatch.End();
    }


    public void AddConsoleItem(string str)
    {
        consoleItems.Add(str);
    }


    public void RegisterCommand(
        string name,
        Action<List<string>> callback)
    {
        commands[name] = callback;
    }


    public void ExecuteCommand(string input)
    {
        var tokens =
            input.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries)
                .ToList();


        if (tokens.Count == 0)
            return;


        string command = tokens[0];


        List<string> args =
            tokens.Skip(1).ToList();


        if (!commands.TryGetValue(command, out var callback))
        {
            consoleItems.Add(
                "Unknown command: " + command);
            
            return;
        }


        callback(args);
    }
}