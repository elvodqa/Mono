namespace Mono.Util;

public class Level
{
    public List<Entity> Entities { get; private set; } = new();

    public void AddEntity(Entity entity)
    {
        Entities.Add(entity);
        entity.Level = this;
    }
}