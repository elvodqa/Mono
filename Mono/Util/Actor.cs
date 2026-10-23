using System.Drawing;
using OpenTK.Mathematics;

namespace Mono.Util;

public class Actor : Entity
{
    public void Move(Vector2i amount, Action? onCollide = null)
    {
        MoveX(amount.X, onCollide);
        MoveY(amount.Y, onCollide);
    }
    
    public void MoveX(int amount, Action? onCollide = null)
    {
        int step = Math.Sign(amount);

        while (amount != 0)
        {
            if (!CollidesAll(Position.X + step, Position.Y))
            {
                Position.X += step;
                amount -= step;
            }
            else
            {
                onCollide?.Invoke();
                break;
            }
        }
    }

    public void MoveY(int amount, Action? onCollide = null)
    {
        int step = Math.Sign(amount);

        while (amount != 0)
        {
            if (!CollidesAll(Position.X, Position.Y + step))
            {
                Position.Y += step;
                amount -= step;
            }
            else
            {
                onCollide?.Invoke();
                break;
            }
        }
    }

    private bool CollidesAll(int x, int y)
    {
        int left = x - BboxSize.X / 2;
        int right = x + BboxSize.X / 2;
        int top = y - BboxSize.Y / 2;
        int bottom = y + BboxSize.Y / 2;

        foreach (var solid in Level.Entities.OfType<Solid>().Where(solid => solid.Collidable))
        {
            if (right > solid.BboxLeft &&
                left < solid.BboxRight &&
                bottom > solid.BboxTop &&
                top < solid.BboxBottom)
            {
                return true;
            }
        }

        return false;
    }
    
    /// <summary>
    /// Default implementation assumes the riding for a platformer.
    /// </summary>
    /// <param name="solid"></param>
    public virtual bool IsRiding(Solid solid)
    {
        for (int i = BboxLeft; i <= BboxRight; i++)
        {
            if (i >= solid.BboxLeft && i <= solid.BboxRight && Math.Abs(BboxBottom - solid.BboxTop) < 2)
            {
                return true;
            }
        }
        
        return false;
    }
    
    public virtual void Squish() {}
}