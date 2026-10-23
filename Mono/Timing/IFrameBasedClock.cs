namespace Mono.Timing;

public interface IFrameBasedClock : IClock
{
    double ElapsedFrameTime { get; }
    double AverageFrameTime { get; }
    double FramesPerSecond { get; }
    void ProcessFrame();
}