namespace Mono.UI;

public class UIManager
{
    public List<UIElement> Elements = new();
    
    public UIElement Hovered;
    public UIElement Captured;
    
    private UIElement? GetTopElement(float x, float y)
    {
        for (int i = Elements.Count - 1; i >= 0; i--)
        {
            var v = Elements[i];
            if (v is Container container)
            {
                var c = container.GetTopElement(x, y);
                if (c != null) return c;
            }
            if (v.HitTest(x, y))
                return v;
        }
        return null;
    }

    public void Update(double delta)
    {
        var top = GetTopElement(Input.MouseX, Input.MouseY);
        
        
    }
}
