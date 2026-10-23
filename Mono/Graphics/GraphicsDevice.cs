using System.Drawing;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using SDL;
using static SDL.SDL3;

namespace Mono.Graphics;

public enum Primitive {
    Triangles,
    TriangleFan,
}

public enum ElementType
{
    UnsignedInt,
    UnsignedShort,
    UnsignedByte,
}

public unsafe class GraphicsDevice
{
    public Buffer VertexBuffer { set; get; }
    public Buffer IndexBuffer { set; get; }
    public Vector2i FramebufferSize => Window.SizeInPixels;
    public Window Window { get; private set; }
    private Stack<RenderTarget> _renderTargetStack = new Stack<RenderTarget>();
    public RenderTarget? RenderTarget { get; internal set; }

    private Stack<Rectangle> _scissorStack = new Stack<Rectangle>();
    public Rectangle Scissor { get; internal set; }


    public GraphicsDevice(Window window)
    {
        Window = window;
    }
    
    public void DrawIndexedPrimitives(Primitive primitive, ElementType elementType, int baseVertex, int vertexCount)
    {
        GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBuffer.Handle);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, IndexBuffer.Handle);
        GL.DrawElementsBaseVertex(toOpenGlPrimitiveType(primitive), vertexCount, toDrawElementsType(elementType), 0, baseVertex);
    }
    
    public void Clear(Color4<Rgba> color)
    {
        GL.ClearColor(color);
        GL.Clear(ClearBufferMask.ColorBufferBit);
    }

    public void SetViewport(Rectangle viewport)
    {
        int height;
        if (RenderTarget == null) height = (int)FramebufferSize.Y;
        else height = RenderTarget.Height;
        
        int glY = height - viewport.Y - viewport.Height;
        GL.Viewport(viewport.X, glY, viewport.Width, viewport.Height);
    }

    
      public void PushRenderTarget(RenderTarget renderTarget)
    {
        _renderTargetStack.Push(renderTarget);
        SetRenderTarget(renderTarget);
    }

    public void PopRenderTarget()
    {
        if (_renderTargetStack.Count == 0)
        {
            SetRenderTarget(null);
            return;
        }

        _renderTargetStack.Pop();

        if (_renderTargetStack.Count > 0)
            SetRenderTarget(_renderTargetStack.Peek());
        else
            SetRenderTarget(null);
    }

    private void SetRenderTarget(RenderTarget? renderTarget)
    {
        RenderTarget = renderTarget;

        if (renderTarget != null)
        {
            renderTarget.Bind();
        }
        else
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }
    }

    public void PushScissor(Rectangle scissor)
    {
        if (_scissorStack.Count > 0)
        {
            scissor = Rectangle.Intersect(
                _scissorStack.Peek(),
                scissor);
        }

        _scissorStack.Push(scissor);
        SetScissor(scissor);
    }

    public void PopScissor()
    {
        if (_scissorStack.Count == 0)
        {
            GL.Disable(EnableCap.ScissorTest);
            Scissor = Rectangle.Empty;
            return;
        }

        var previous = _scissorStack.Pop();

        if (_scissorStack.Count == 0)
        {
            GL.Disable(EnableCap.ScissorTest);
            Scissor = Rectangle.Empty;
        }
        else if (previous.Width > 0 && previous.Height > 0)
        {
            SetScissor(previous);
        }
        else
        {
            GL.Disable(EnableCap.ScissorTest);
            Scissor = Rectangle.Empty;
        }
    }

    public void SetScissor(Rectangle scissor)
    {
        GL.Enable(EnableCap.ScissorTest);

        int height;
        if (RenderTarget == null) height = FramebufferSize.Y;
        else height = RenderTarget.Height;
        
        GL.Scissor(
            scissor.X,
            height - scissor.Y - scissor.Height,
            scissor.Width,
            scissor.Height
        );

        Scissor = scissor;
    }


    private DrawElementsType toDrawElementsType(ElementType elementType)
    {
        return elementType switch
        {
            ElementType.UnsignedByte => DrawElementsType.UnsignedByte,
            ElementType.UnsignedInt => DrawElementsType.UnsignedInt,
            ElementType.UnsignedShort => DrawElementsType.UnsignedShort,
        };
    }

    private PrimitiveType toOpenGlPrimitiveType(Primitive primitive)
    {
        return primitive switch
        {
            Primitive.Triangles => PrimitiveType.Triangles,
            Primitive.TriangleFan => PrimitiveType.TriangleFan,
        };
    }
}