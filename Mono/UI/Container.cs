namespace Mono.UI;

public class Container : UIElement
{
    public List<UIElement> Children { get; private set; } = new();
    
    public virtual void UpdateLayout() {}
    
    internal UIElement? GetTopElement(float x, float y)
    {
        for (int i = Children.Count - 1; i >= 0; i--)
        {
            var v = Children[i];
            if (v.HitTest(x, y))
                return v;
        }
        return null;
    }
}