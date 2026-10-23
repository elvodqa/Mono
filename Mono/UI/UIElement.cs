namespace Mono.UI;

public class UIElement : IInputReceiver
{
    public UIElement? Parent;
    public float X { get; }
    public float Y { get; }
    public float Width { get; }
    public float Height { get; }
    public virtual bool HitTest(float mouseX, float mouseY)
    {
        return mouseX >= X && mouseX <= X + Width && mouseY >= Y && mouseY <= Y + Height;
    }

    public virtual void OnMouseScroll(float wheelX, float wheelY)
    {
        
    }

    public virtual void OnMouseEnter()
    {
        
    }

    public virtual void OnMouseLeave()
    {
        
    }

    public virtual void OnMouseMove(MouseEventArgs e)
    {
        
    }

    public virtual void OnMouseDown(MouseButton button, MouseEventArgs e)
    {
        
    }

    public virtual void OnMouseUp(MouseButton button, MouseEventArgs e)
    {
        
    }

    public virtual void OnKeyboardReceived(string input)
    {
        
    }
}