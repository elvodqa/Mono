using OpenTK.Mathematics;

namespace Mono.Util;

public class Camera2D
{
    private Vector2 _viewportSize;
    private Vector2 _position;
    private float _zoom = 1f;

    public Matrix4 View { get; private set; } = Matrix4.Identity;

    public Vector2 ViewportSize
    {
        get => _viewportSize;
        set
        {
            _viewportSize = value;
            Update();
        }
    }

    public Vector2 Position
    {
        get => _position;
        set
        {
            _position = value;
            Update();
        }
    }

    public float Zoom
    {
        get => _zoom;
        set
        {
            _zoom = MathF.Max(value, 0.0001f);
            Update();
        }
    }

    public Camera2D(Vector2 viewportSize)
    {
        _viewportSize = viewportSize;
        Update();
    }

    public Vector2 ScreenToWorld(Vector2 screenPos)
    {
        Matrix4 inv = View.Inverted();

        Vector4 world = new Vector4(screenPos.X, screenPos.Y, 0f, 1f) * inv;

        if (world.W != 0f)
            world /= world.W;

        return world.Xy;
    }

    public Vector2 WorldToScreen(Vector2 worldPos)
    {
        Vector4 screen = new Vector4(worldPos.X, worldPos.Y, 0f, 1f) * View;
        return screen.Xy;
    }

    private void Update()
    {
        View =
            Matrix4.CreateTranslation(ViewportSize.X * 0.5f, ViewportSize.Y * 0.5f, 0f) *
            Matrix4.CreateScale(Zoom, Zoom, 1f) *
            Matrix4.CreateTranslation(-Position.X, -Position.Y, 0f);
    }
}