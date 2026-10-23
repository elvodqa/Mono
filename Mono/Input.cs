using System.Runtime.InteropServices;

namespace Mono;

using SDL;
using static SDL.SDL3;

public unsafe class Input
{
    private static int _numOfKeys;

    private static SDL_MouseButtonFlags _currentMouse;
    private static SDL_MouseButtonFlags _previousMouse;
    
    private static byte[] _currentKeyboard = new byte[(int)SDL_Scancode.SDL_SCANCODE_COUNT];
    private static byte[] _previousKeyboard = new byte[(int)SDL_Scancode.SDL_SCANCODE_COUNT];

    public static float MouseX { get; private set; }
    public static float MouseY { get; private set; }
    public static float DeltaX { get; private set; }
    public static float DeltaY { get; private set; }
    public static float WheelX { get; private set; }
    public static float WheelY { get; private set; }

    public static bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    private static bool _enabled = true;
    
    public static byte[] GetKeyboardState()
    {
        fixed (int* keys = &_numOfKeys)
        {
            // _previousKeyState = _currentKeyState;
            // _currentKeyState =  SDL_GetKeyboardState(keys);
            Array.Copy(_currentKeyboard, _previousKeyboard, _numOfKeys);
            Marshal.Copy((nint)SDL_GetKeyboardState(keys), _currentKeyboard, 0, _numOfKeys);
            return _currentKeyboard;
        }
    }

    public static void FixScrollback()
    {
        WheelX = 0;
        WheelY = 0;
        _textInput = "";
    }

    public static void UpdateEvents(SDL_Event ev)
    {
        switch (ev.type)
        {
            case (uint)SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
                WheelX = ev.wheel.x;
                WheelY = ev.wheel.y;
                break;

            case (uint)SDL_EventType.SDL_EVENT_TEXT_INPUT:
            {
                // SDL sends UTF-8 text
                _textInput += Marshal.PtrToStringUTF8(
                    (nint)ev.text.text
                );

                break;
            }
        }
    }

    public static void UpdateMouse(SDL_Window* window)
    {
        // store previous state
        _previousMouse = _currentMouse;

        float x, y;
        _currentMouse = SDL_GetMouseState(&x, &y);
        
        float scale = SDL_GetWindowDisplayScale(window);

        // position
        float prevX = MouseX;
        float prevY = MouseY;

        MouseX = x * scale;
        MouseY = y * scale;

        // delta
        DeltaX = MouseX - prevX;
        DeltaY = MouseY - prevY;
    }
    
    // private static SDL_MouseButtonFlags ButtonMask(SDL_MouseButtonFlags button)
    // {
    //     return (SDL_MouseButtonFlags)(1 << ((int)button - 1));
    // }

    public static bool IsKeyDown(SDL_Scancode scancode)
    {
        if (!Enabled) return false;
        return _currentKeyboard[(int)scancode] != 0;
    }

    public static bool IsKeyPressed(SDL_Scancode scancode)
    {
        if (!Enabled) return false;
        return _currentKeyboard[(int)scancode] != 0 && _previousKeyboard[(int)scancode] == 0;
    }
    
    public static bool IsKeyReleased(SDL_Scancode scancode)
    {
        if (!Enabled) return false;
        return _currentKeyboard[(int)scancode] == 0 && _previousKeyboard[(int)scancode] != 0;
    }
    
    public static bool IsMouseDown(SDLButton button)
    {
        if (!Enabled) return false;
        return (_currentMouse & SDL_BUTTON(button)) != 0;
    }
    
    public static bool IsMousePressed(SDLButton button)
    {
        if (!Enabled) return false;
        var mask = SDL_BUTTON(button);

        return (_currentMouse & mask) != 0 &&
               (_previousMouse & mask) == 0;
    }
    
    public static bool IsMouseReleased(SDLButton button)
    {
        if (!Enabled) return false;
        var mask = SDL_BUTTON(button);

        return (_currentMouse & mask) == 0 &&
               (_previousMouse & mask) != 0;
    }
    
    private static string _textInput = "";

    public static string TextInput => _textInput;

    public static void BeginTextInput(SDL_Window* window)
    {
        SDL_StartTextInput(window);
    }

    public static void EndTextInput(SDL_Window* window)
    {
        SDL_StopTextInput(window);
    }

    public static void ClearTextInput()
    {
        _textInput = "";
    }
}