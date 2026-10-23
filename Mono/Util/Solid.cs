using System.Drawing;
using OpenTK.Mathematics;

namespace Mono.Util;

public class Solid : Entity
{
    public bool Collidable = true;
    
    public List<Actor> AllRidingActors =>
        Level.Entities
            .OfType<Actor>()
            .Where(actor => actor.IsRiding(this))
            .ToList();


    private float xRemainder = 0;
    private float yRemainder = 0;
    public void Move(float x, float y)
    {
        xRemainder += x;
        yRemainder += y;   
        int moveX = (int)Math.Round(xRemainder);
        int moveY = (int)Math.Round(yRemainder);
        if (moveX != 0 || moveY != 0)
        {
            // Loop through every Actor in the Level, add it to
            // a list if actor.IsRiding(this) is true
            var riding = AllRidingActors;

            // Make this Solid non-collidable for Actors,
            // so that Actors moved by it do not get stuck on it
            Collidable = false;

            if (moveX != 0)
            {
                xRemainder -= moveX;
                Position.X += moveX;
                if (moveX > 0)
                {
                    foreach (Entity entity in Level.Entities)
                    {
                        if (entity is Actor actor)
                        {
                            if (overlapCheck(actor))
                            {
                                // Push right
                                actor.MoveX(BboxRight - actor.BboxLeft, actor.Squish);
                            }
                            else if (riding.Contains(actor))
                            {
                                // Carry right
                                actor.MoveX(moveX, null);
                            }
                        }
                    }
                }
                else
                {
                    foreach (Entity entity in Level.Entities)
                    {
                        if (entity is Actor actor)
                        {
                            if (overlapCheck(actor))
                            {
                                // Push left
                                actor.MoveX(BboxLeft - actor.BboxRight, actor.Squish);
                            }
                            else if (riding.Contains(actor))
                            {
                                // Carry left
                                actor.MoveX(moveX, null);
                            }
                        }
                    }
                }
            }
        
            if (moveY != 0)
            {
                yRemainder -= moveY;
                Position.Y += moveY;
                if (moveY > 0)
                {
                    foreach (Entity entity in Level.Entities)
                    {
                        if (entity is Actor actor)
                        {
                            if (overlapCheck(actor))
                            {
                                // Push right
                                actor.MoveY(BboxBottom - actor.BboxTop, actor.Squish);
                            }
                            else if (riding.Contains(actor))
                            {
                                // Carry right
                                actor.MoveY(moveY, null);
                            }
                        }
                    }
                }
                else
                {
                    foreach (Entity entity in Level.Entities)
                    {
                        if (entity is Actor actor)
                        {
                            if (overlapCheck(actor))
                            {
                                // Push left
                                actor.MoveY(BboxTop - actor.BboxBottom, actor.Squish);
                            }
                            else if (riding.Contains(actor))
                            {
                                // Carry left
                                actor.MoveY(moveY, null);
                            }
                        }
                    }
                }
            }
        
            // Re-enable collisions for this Solid
            Collidable = true;
        }
    }
    
    private bool overlapCheck(Actor actor)
    {
        Rectangle rect1 = new Rectangle((int)(actor.Position.X - actor.BboxSize.X / 2f),
            (int)(actor.Position.Y - actor.BboxSize.Y / 2f), actor.BboxSize.X, actor.BboxSize.Y);
        Rectangle rect2 = new Rectangle((int)(Position.X - BboxSize.X / 2f),
            (int)(Position.Y - BboxSize.Y / 2f), BboxSize.X, BboxSize.Y);

        return rect1.IntersectsWith(rect2);
    }
}