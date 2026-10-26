using System.Drawing;
using Mono.Graphics;
using Mono.Util;
using OpenTK.Mathematics;
using SDL;

namespace Mono.GameTest;

public class Player : Actor
{
    private const float MaxSpeed = 0.45f;
    private const float GroundAcceleration = 0.0048f;
    private const float AirAcceleration = 0.0024f;
    private const float GroundFriction = 0.006f;
    private const float AirFriction = 0.0009f;
    private const float Gravity = 0.005f;
    private const float MaxFallSpeed = 1.6f;
    private const float JumpSpeed = 1.45f;
    private const float JumpCut = 0.4f;
    private const float CoyoteTime = 80f;
    private const float JumpBufferTime = 110f;

    private readonly Texture _idleTexture;
    private readonly Texture _walkATexture;
    private readonly Texture _walkBTexture;
    private readonly Texture _jumpTexture;

    private float _xRemainder;
    private float _yRemainder;
    private float _coyoteTimer;
    private float _jumpBuffer;
    private float _animationTime;

    public Vector2 Velocity;
    public int Facing = 1;
    public bool Grounded { get; private set; }
    public float Invulnerable { get; set; }

    public Action? Jumped;

    public bool OnFloor => Level != null && Level.Entities.Any(entity =>
        entity is Solid solid &&
        solid.Collidable &&
        IsRiding(solid));

    public Player(Texture idleTexture, Texture walkATexture, Texture walkBTexture, Texture jumpTexture)
    {
        _idleTexture = idleTexture;
        _walkATexture = walkATexture;
        _walkBTexture = walkBTexture;
        _jumpTexture = jumpTexture;
        BboxSize = new Vector2i(48, 58);
    }

    public void Update(double elapsedFrameTime)
    {
        if (Level == null) return;

        float dt = Math.Clamp((float)elapsedFrameTime, 0f, 32f);
        if (dt <= 0f) return;

        _animationTime += dt;
        if (Invulnerable > 0f) Invulnerable = MathF.Max(0f, Invulnerable - dt);

        _coyoteTimer -= dt;
        _jumpBuffer -= dt;

        if (Grounded) _coyoteTimer = CoyoteTime;

        if (Input.IsKeyPressed(SDL_Scancode.SDL_SCANCODE_SPACE) ||
            Input.IsKeyPressed(SDL_Scancode.SDL_SCANCODE_W) ||
            Input.IsKeyPressed(SDL_Scancode.SDL_SCANCODE_UP))
        {
            _jumpBuffer = JumpBufferTime;
        }

        if (_jumpBuffer > 0f && _coyoteTimer > 0f)
        {
            Velocity.Y = -JumpSpeed;
            _jumpBuffer = 0f;
            _coyoteTimer = 0f;
            Grounded = false;
            Jumped?.Invoke();
        }

        bool jumpHeld = Input.IsKeyDown(SDL_Scancode.SDL_SCANCODE_SPACE) ||
                        Input.IsKeyDown(SDL_Scancode.SDL_SCANCODE_W) ||
                        Input.IsKeyDown(SDL_Scancode.SDL_SCANCODE_UP);

        if (!jumpHeld && Velocity.Y < 0f)
            Velocity.Y = MathF.Max(Velocity.Y, -JumpSpeed * JumpCut);

        float direction = Convert.ToInt32(Input.IsKeyDown(SDL_Scancode.SDL_SCANCODE_D) ||
                                          Input.IsKeyDown(SDL_Scancode.SDL_SCANCODE_RIGHT)) -
                          Convert.ToInt32(Input.IsKeyDown(SDL_Scancode.SDL_SCANCODE_A) ||
                                          Input.IsKeyDown(SDL_Scancode.SDL_SCANCODE_LEFT));

        if (direction != 0f)
        {
            float acceleration = Grounded ? GroundAcceleration : AirAcceleration;
            Velocity.X = Math.Clamp(Velocity.X + direction * acceleration * dt, -MaxSpeed, MaxSpeed);
            Facing = direction > 0f ? 1 : -1;
        }
        else
        {
            float friction = (Grounded ? GroundFriction : AirFriction) * dt;
            if (MathF.Abs(Velocity.X) <= friction)
                Velocity.X = 0f;
            else
                Velocity.X -= MathF.Sign(Velocity.X) * friction;
        }

        Velocity.Y = MathF.Min(Velocity.Y + Gravity * dt, MaxFallSpeed);

        MoveHorizontal(Velocity.X * dt);
        MoveVertical(Velocity.Y * dt);

        if (MathF.Abs(Velocity.X) < 0.01f) Velocity.X = 0f;
    }

    private void MoveHorizontal(float amount)
    {
        _xRemainder += amount;
        int move = (int)MathF.Round(_xRemainder);

        if (move == 0) return;

        _xRemainder -= move;
        int before = Position.X;
        MoveX(move);

        if (Position.X - before != move)
            Velocity.X = 0f;
    }

    private void MoveVertical(float amount)
    {
        _yRemainder += amount;
        int move = (int)MathF.Round(_yRemainder);

        if (move == 0) return;

        _yRemainder -= move;
        int before = Position.Y;
        MoveY(move);
        int moved = Position.Y - before;

        if (moved != move)
        {
            if (move > 0) Grounded = true;
            Velocity.Y = 0f;
        }
        else if (move > 0)
        {
            Grounded = false;
        }

        if (OnFloor) Grounded = true;
    }

    public void Respawn(Vector2i position)
    {
        Position = position;
        Velocity = Vector2.Zero;
        _xRemainder = 0f;
        _yRemainder = 0f;
        _jumpBuffer = 0f;
        _coyoteTimer = 0f;
        Grounded = false;
        Facing = 1;
        Invulnerable = 1200f;
    }

    public void Bounce()
    {
        Velocity.Y = -JumpSpeed * 0.62f;
        Grounded = false;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (Invulnerable > 0f && (int)(Invulnerable / 85f) % 2 == 0)
            return;

        Texture texture;

        if (!Grounded && MathF.Abs(Velocity.Y) > 0.08f)
            texture = _jumpTexture;
        else if (MathF.Abs(Velocity.X) > 0.03f)
            texture = (int)(_animationTime / 105f) % 2 == 0 ? _walkATexture : _walkBTexture;
        else
            texture = _idleTexture;

        const float size = 76f;
        float centerX = Position.X;
        float centerY = Position.Y - 9f;
        bool flip = Facing < 0;

        spriteBatch.Draw(
            texture,
            new RectangleF(centerX, centerY, flip ? -size : size, size),
            Color4.White,
            0f,
            new Vector2(0.5f, 0.5f));
    }
}
