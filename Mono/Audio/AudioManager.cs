using System.Runtime.InteropServices;
using Mono.Storage;
using SDL;
using static SDL.SDL3;
using static SDL.SDL3_mixer;

namespace Mono.Audio;


public static unsafe class AudioManager
{
    private static readonly object _lock = new();

    public static List<Track> Tracks { get; } = new();
    public static List<Audio> Audios { get; } = new();

    private static MIX_Mixer* _mixer;

    public static Queue<Track> SampleTracks { get; } = new();

    private const int MaxSamples = 50;

    public static float SampleVolume { get; set; } = 1;
    public static float SampleSpeed { get; set; } = 1;

    private static double _counter;

    static AudioManager()
    {
        _mixer = MIX_CreateMixerDevice(SDL_AUDIO_DEVICE_DEFAULT_PLAYBACK, null);

        if (_mixer == null)
        {
            throw new Exception("Failed to create mixer device.");
        }
    }

    public static Audio LoadAudio(TitleStorage storage, string path)
    {
        if (!storage.Exists(path))
            throw new Exception("Song not found: " + path);

        storage.GetFileSize(path, out var size);

        var data = new byte[size];
        storage.ReadFile(path, data);

        GCHandle pinned = GCHandle.Alloc(data, GCHandleType.Pinned);

        try
        {
            IntPtr ptr = pinned.AddrOfPinnedObject();

            var handle = MIX_LoadAudioNoCopy(_mixer, ptr, (UIntPtr)size, false);

            if (handle == null)
                Logger.Fatal($"Error loading audio: {SDL_GetError()}");

            var audioChannel = new Audio
            {
                Path = path,
                Handle = handle,
                Data = data,
                PinHandle = pinned // keep it alive
            };

            Audios.Add(audioChannel);

            return audioChannel;
        }
        catch
        {
            pinned.Free();
            throw;
        }
    }

    public static Track CreateTrack()
    {
        Track track = new()
        {
            Handle = MIX_CreateTrack(_mixer),
            Audio = null
        };

        Tracks.Add(track);

        return track;
    }

    public static void PlaySample(Audio audio)
    {
        lock (_lock)
        {
            while (SampleTracks.Count >= MaxSamples)
            {
                var oldTrack = SampleTracks.Dequeue();

                try
                {
                    oldTrack.Stop();
                }
                catch
                {
                    // ignore
                }

                oldTrack.Dispose();
            }

            Track track = CreateTrack();
            track.Audio = audio;
            track.Volume = SampleVolume;
            track.Speed = SampleSpeed;
            track.Play();

            SampleTracks.Enqueue(track);
        }
    }

    public static void Update(double delta)
    {
        _counter += delta;

        if (_counter < 100)
            return;

        _counter = 0;

        lock (_lock)
        {
            int count = SampleTracks.Count;

            for (int i = 0; i < count; i++)
            {
                var track = SampleTracks.Dequeue();

                if (track.Playing)
                {
                    SampleTracks.Enqueue(track);
                }
                else
                {
                    track.Dispose();
                }
            }
        }
    }

    public static void Shutdown()
    {
        lock (_lock)
        {
            while (SampleTracks.Count > 0)
            {
                SampleTracks.Dequeue().Dispose();
            }

            MIX_DestroyMixer(_mixer);
            _mixer = null;
        }
    }
}