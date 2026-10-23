using OpenTK.Mathematics;
using SDL;
using static SDL.SDL3;

namespace Mono;

public unsafe class Window : IDisposable
{
    public SDL_Window* Handle { get; private set; }
    
    public Vector2i SizeInPixels
    {
        get
        {
            int w, h;
            SDL_GetWindowSizeInPixels(Handle, &w, &h);
            return new Vector2i(w, h);
        }
    }

    public Vector2i Size
    {
        get
        {
            int w, h;
            SDL_GetWindowSize(Handle, &w, &h);
            return new Vector2i(w, h);
        }
        set => SDL_SetWindowSize(Handle, value.X, value.Y);
    }

    public string Title
    {
        get => SDL_GetWindowTitle(Handle)!;
        set => SDL_SetWindowTitle(Handle, value);
    }

    public bool Resizable
    {
        get
        {
            var flags = SDL_GetWindowFlags(Handle);
            return (flags & SDL_WindowFlags.SDL_WINDOW_RESIZABLE) != 0;
        }
        set => SDL_SetWindowResizable(Handle, value);
    }

    public bool CursorVisible;

    public Window(string title, Vector2i size)
    {
        Handle = SDL_CreateWindow(title, size.X, size.Y, SDL_WindowFlags.SDL_WINDOW_OPENGL
                                                     | SDL_WindowFlags.SDL_WINDOW_RESIZABLE
                                                     | SDL_WindowFlags.SDL_WINDOW_HIGH_PIXEL_DENSITY
        );
    }

    public void Maximize() => SDL_MaximizeWindow(Handle);

    public void Dispose()
    {
        SDL_DestroyWindow(Handle);
    }
}