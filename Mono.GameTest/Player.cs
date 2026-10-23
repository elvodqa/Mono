using Mono.Util;
using OpenTK.Mathematics;
using SDL;

namespace Mono.GameTest;

public class Player : Actor
{
    public const int SPEED = 4;
    public const float GRAVITY_MAX = 1.0f;

    public float Gravity = GRAVITY_MAX;
    public float JumpForce = 0.0f;
    public bool Jumping = false;

    public Vector2 Velocity;
    public Vector2 Acceleration;

    public bool OnFloor => Level.Entities.Any(entity =>
        entity is Solid solid &&
        solid.Collidable &&
        IsRiding(solid));

    public void Update(double dt)
    {
        // Reset horizontal velocity each frame.
        Velocity = Vector2.Zero;

        // Reset the jump state when landing.
        if (Jumping && OnFloor)
        {
            Jumping = false;
            Gravity = GRAVITY_MAX;
        }

        // Start a jump only when on the floor.
        if (Input.IsKeyPressed(SDL_Scancode.SDL_SCANCODE_SPACE) && OnFloor)
        {
            Jumping = true;
            Gravity = 0;
            JumpForce = 10.0f;
        }

        // Reduce jump force over time.
        if (Jumping)
            JumpForce /= 1.2f;

        // End the jump when its force becomes small enough.
        if (Jumping && JumpForce <= 1.0f)
        {
            Jumping = false;
            Gravity = GRAVITY_MAX;
        }

        // Horizontal movement.
        if (Input.IsKeyDown(SDL_Scancode.SDL_SCANCODE_A))
            Velocity.X -= SPEED;

        if (Input.IsKeyDown(SDL_Scancode.SDL_SCANCODE_D))
            Velocity.X += SPEED;

        // Vertical movement.
        Velocity.Y = Jumping ? -JumpForce : Gravity;

        MoveX((int)(Velocity.X * dt));
        MoveY((int)(Velocity.Y * dt));
    }

    public void Draw()
    {
    }
}