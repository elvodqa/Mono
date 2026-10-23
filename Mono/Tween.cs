namespace Mono;

public class Tween
{
    public int id;

    public Action<float>? setter;

    public float from;
    public float to;

    public float duration;
    public float elapsed;

    public bool completed;

    public TweenOptions options;

    // Runtime state
    public int remainingLoops;
    public bool reversing;
}

public class TweenOptions
{
    public Func<float, float> Func = t => t;

    public bool Yoyo = false;

    // -1 = infinite
    public int Loops = 0;

    public Action? Callback = null;
}

public static class TweenSystem
{
    private static int s_nextTweenId = 0;
    private static readonly List<Tween> m_activeTweens = new();


    public static int Tween(
        Action<float> setter,
        float from,
        float to,
        float duration,
        TweenOptions options)
    {
        Tween tween = new();

        tween.id = s_nextTweenId++;

        tween.setter = setter;
        tween.from = from;
        tween.to = to;

        tween.duration = duration;
        tween.elapsed = 0f;

        tween.options = options;
        
        tween.remainingLoops = options.Loops;
        tween.reversing = false;

        m_activeTweens.Add(tween);

        return tween.id;
    }


    public static void Adjust(
        int id,
        float from,
        float to,
        float duration)
    {
        var tween = m_activeTweens
            .FirstOrDefault(t => t.id == id);

        if (tween == null)
            return;


        tween.from = from;
        tween.to = to;

        tween.duration = duration;
        tween.elapsed = 0f;

        tween.completed = false;

        tween.remainingLoops = tween.options.Loops;
        tween.reversing = false;

        tween.setter?.Invoke(from);
    }


    public static bool Stop(int id)
    {
        var tween = m_activeTweens
            .FirstOrDefault(t => t.id == id);

        if (tween == null)
            return false;


        tween.completed = true;
        return true;
    }


    public static bool Exists(int id)
    {
        return m_activeTweens.Any(t => t.id == id);
    }


    public static bool IsRunning(int id)
    {
        var tween = m_activeTweens
            .FirstOrDefault(t => t.id == id);

        return tween != null && !tween.completed;
    }


    internal static void Update(double dt)
    {
        foreach (var tween in m_activeTweens)
        {
            if (tween.completed)
                continue;


            tween.elapsed += (float)dt/1000.0f;


            float t = tween.elapsed / tween.duration;
            t = Math.Clamp(t, 0f, 1f);


            float eased = tween.options.Func(t);


            float value =
                tween.from +
                (tween.to - tween.from) * eased;


            tween.setter?.Invoke(value);


            if (t >= 1f)
            {
                if (tween.options.Yoyo)
                {
                    if (!tween.reversing)
                    {
                        // Forward finished, play backward.
                        tween.reversing = true;
                        (tween.from, tween.to) = (tween.to, tween.from);
                        tween.elapsed = 0f;
                    }
                    else
                    {
                        // Backward finished, one full loop complete.
                        tween.reversing = false;

                        if (tween.remainingLoops == -1)
                        {
                            (tween.from, tween.to) = (tween.to, tween.from);
                            tween.elapsed = 0f;
                        }
                        else if (tween.remainingLoops > 0)
                        {
                            tween.remainingLoops--;
                            (tween.from, tween.to) = (tween.to, tween.from);
                            tween.elapsed = 0f;
                        }
                        else
                        {
                            tween.setter?.Invoke(tween.to);
                            tween.completed = true;
                            tween.options.Callback?.Invoke();
                        }
                    }
                }
                else
                {
                    if (tween.remainingLoops == -1)
                    {
                        tween.elapsed = 0f;
                        tween.setter?.Invoke(tween.from);
                    }
                    else if (tween.remainingLoops > 0)
                    {
                        tween.remainingLoops--;

                        tween.elapsed = 0f;
                        tween.setter?.Invoke(tween.from);
                    }
                    else
                    {
                        tween.setter?.Invoke(tween.to);
                        tween.completed = true;
                        tween.options.Callback?.Invoke();
                    }
                }
            }
        }


        m_activeTweens.RemoveAll(t => t.completed);
    }
}