using System.Runtime.InteropServices;
using SDL;
using static SDL.SDL3;
using static SDL.SDL3_mixer;

namespace Mono.Audio;

public unsafe class Audio : IDisposable
{
    public MIX_Audio* Handle;
    public string Path;
    
    public double Length
    {
        get
        {
            var frames = MIX_GetAudioDuration(Handle);
            return MIX_AudioFramesToMS(Handle, frames);
        }
    }

    public Memory<byte> Data { get; set; }
    public GCHandle PinHandle { get; set; }

    public void Dispose()
    {
        PinHandle.Free();
        
        AudioManager.Audios.Remove(this);
        MIX_DestroyAudio(Handle);
    }
}