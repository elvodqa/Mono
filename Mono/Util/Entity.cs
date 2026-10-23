using System.Drawing;
using OpenTK.Mathematics;

namespace Mono.Util;

public class Entity
{
    public Level Level;
    public Vector2i Position;
    public Vector2i BboxSize;

    public int BboxLeft => Position.X - BboxSize.X / 2;
    public int BboxRight => Position.X + BboxSize.X / 2;
    public int BboxTop => Position.Y - BboxSize.Y / 2;
    public int BboxBottom => Position.Y + BboxSize.Y / 2;
    
    public virtual bool Overlaps(Entity entity)
    {
        Rectangle rect1 = new Rectangle((int)(entity.Position.X - entity.BboxSize.X / 2f),
            (int)(entity.Position.Y - entity.BboxSize.Y / 2f), entity.BboxSize.X, entity.BboxSize.Y);
        Rectangle rect2 = new Rectangle((int)(Position.X - BboxSize.X / 2f),
            (int)(Position.Y - BboxSize.Y / 2f), BboxSize.X, BboxSize.Y);

        return rect1.IntersectsWith(rect2);
    }
}