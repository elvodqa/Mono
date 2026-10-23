using SDL;

namespace Mono.Storage;

using static SDL3;

public unsafe class TitleStorage : IDisposable
{
    public SDL_Storage* Handle { get; private set; } = null;

    public TitleStorage()
    {
        Open();
    }
    
    public bool Exists(string path)
    {
        return SDL_GetStoragePathInfo(Handle, path, null);
    }
    
    /// <summary>
    /// Query the size of a file within a storage container.
    /// </summary>
    /// <param name="path">A path relative to the title root.</param>
    /// <param name="size">Filled in with the size of the file.</param>
    /// <returns>True if the query succeeded, false otherwise.</returns>
    public bool GetFileSize(string path, out ulong size)
    {
        ulong _size = 0;
        if (!SDL_GetStorageFileSize(Handle, path, &_size))
        {
            Logger.Fatal($"File at {path} failed to load: {SDL_GetError()}");
            size = 0;
            return false;
        }

        size = _size;
        return true;
    }
    
    /// <summary>
    /// Synchronously read a file into a client-provided Span.
    /// The span must be the same length as the file size.
    /// </summary>
    /// <param name="path">The relative path from the title root.</param>
    /// <returns>True on success, false on failure.</returns>
    public unsafe bool ReadFile(string path, Span<byte> span)
    {
        fixed (byte* ptr = span)
        {
            if (!SDL_ReadStorageFile(Handle, path, (nint) ptr, (ulong) span.Length))
            {
                Logger.Fatal($"File at {path} failed to load: {SDL_GetError()}");
                return false;
            }
        }

        return true;
    }
    
    private void Open()
    {
        if (Handle != null)
        {
            throw new Exception("Storage already open! Close it first!");
        }

        Handle = SDL_OpenTitleStorage("", 0);
        if (Handle == null)
        {
            throw new Exception($"Error creating title storage: {SDL_GetError()}");
        }

        while (!SDL_StorageReady(Handle))
        {
            SDL_Delay(1);
        }
    }

    private void Close()
    {
        if (!SDL_CloseStorage(Handle))
        {
            throw new Exception($"Error closing title storage: {SDL_GetError()}");
        }

        Handle = null;
    }

    public void Dispose()
    {
        if (Handle != null)
        {
            Close();
        }
    }
}