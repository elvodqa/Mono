using System.Drawing;
using Mono.Graphics;
using Mono.Util;
using OpenTK.Mathematics;

namespace Mono.GameTest;

public class Tile : Solid
{
    public Texture Texture = null!;
}

public class Coin : Entity
{
    public bool Collected;
    public float Phase;
}

public class Goal : Entity
{
}

public class Slime : Actor
{
    private const float Speed = 0.075f;
    private const float AnimationFrameTime = 150f;

    private readonly Texture _walkATexture;
    private readonly Texture _walkBTexture;
    private readonly Texture _flatTexture;

    private float _xRemainder;
    private float _animationTime;
    private float _deadTimer;

    public int Direction = -1;
    public int PatrolLeft = int.MinValue;
    public int PatrolRight = int.MaxValue;
    public bool Dead { get; private set; }
    public bool Finished => Dead && _deadTimer <= 0f;

    public Slime(Texture walkATexture, Texture walkBTexture, Texture flatTexture)
    {
        _walkATexture = walkATexture;
        _walkBTexture = walkBTexture;
        _flatTexture = flatTexture;
        BboxSize = new Vector2i(48, 38);
    }

    public void Update(double elapsedFrameTime)
    {
        float dt = Math.Clamp((float)elapsedFrameTime, 0f, 32f);
        if (dt <= 0f) return;

        if (Dead)
        {
            _deadTimer -= dt;
            return;
        }

        _animationTime += dt;

        if (Position.X - BboxSize.X / 2 <= PatrolLeft && Direction < 0)
            Direction = 1;
        else if (Position.X + BboxSize.X / 2 >= PatrolRight && Direction > 0)
            Direction = -1;

        if (!HasGroundAhead())
            Direction = -Direction;

        _xRemainder += Direction * Speed * dt;
        int move = (int)MathF.Round(_xRemainder);

        if (move == 0) return;

        _xRemainder -= move;
        int before = Position.X;
        MoveX(move);

        if (Position.X == before)
            Direction = -Direction;
    }

    private bool HasGroundAhead()
    {
        if (Level == null) return false;

        int direction = Math.Sign(Direction);
        int probeX = Position.X + direction * (BboxSize.X / 2 + 4);
        int probeY = BboxBottom + 6;

        foreach (Solid solid in Level.Entities.OfType<Solid>())
        {
            if (solid.Collidable &&
                probeX >= solid.BboxLeft &&
                probeX <= solid.BboxRight &&
                probeY >= solid.BboxTop &&
                probeY <= solid.BboxBottom)
            {
                return true;
            }
        }

        return false;
    }

    public void Stomp()
    {
        if (Dead) return;

        Dead = true;
        _deadTimer = 450f;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (Finished) return;

        if (Dead)
        {
            spriteBatch.Draw(
                _flatTexture,
                new RectangleF(Position.X - 28f, Position.Y - 37f, 56f, 56f),
                Color4.White,
                0f,
                Vector2.Zero);

            return;
        }

        Texture texture = (int)(_animationTime / AnimationFrameTime) % 2 == 0
            ? _walkATexture
            : _walkBTexture;

        spriteBatch.Draw(
            texture,
            new RectangleF(Position.X - 28f, Position.Y - 37f, 56f, 56f),
            Color4.White,
            0f,
            Vector2.Zero);
    }
}
