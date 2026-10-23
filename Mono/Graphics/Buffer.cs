using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;

namespace Mono.Graphics;

public enum BufferUse
{
    Dynamic,
    Static,
}

public enum BufferType
{
    Vertex,
    Index,
}

public unsafe class Buffer : IDisposable
{
    public GraphicsDevice GraphicsDevice { get; private set; }
    public int Handle { get; private set; }
    public BufferUse Usage { get; private set; }
    public BufferType BufferType { get; private set; }
    public Type ElementType { get; private set; }
    private int elementCount;
    
    public Buffer(GraphicsDevice graphicsDevice, Type type, int elementCount, BufferType bufferType, BufferUse usage)
    {
        this.elementCount = elementCount;
        GraphicsDevice = graphicsDevice;
        BufferType = bufferType;
        Usage = usage;
        Handle = GL.GenBuffer();
        ElementType = type;
        
        Allocate(elementCount);
    }
    
    public void SetData<T>(Span<T> data) where T : unmanaged
    {
        if (data.Length > elementCount) throw new Exception($"Tried to upload buffer data bigger than its capacity ({data.Length} > {elementCount})");
        
        BufferData(data);
        Logger.Trace($"Updated buffer with new data ({data.Length * sizeof(T)} bytes)");
    }
    
    private void Allocate(int elementCount) 
    {
        Bind();

        var bufferUsage = Usage switch
        {
            BufferUse.Static => BufferUsage.StaticDraw,
            BufferUse.Dynamic => BufferUsage.DynamicDraw,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        var bufferTarget = BufferType switch
        {
            BufferType.Index => BufferTarget.ElementArrayBuffer,
            BufferType.Vertex => BufferTarget.ArrayBuffer,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        GL.BufferData(bufferTarget, elementCount * Marshal.SizeOf(ElementType), IntPtr.Zero, bufferUsage);
    }

    private void BufferData<T>(Span<T> data) where T : unmanaged
    {
        Bind();
        
        var bufferUsage = Usage switch
        {
            BufferUse.Static => BufferUsage.StaticDraw,
            BufferUse.Dynamic => BufferUsage.DynamicDraw,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        var bufferTarget = BufferType switch
        {
            BufferType.Index => BufferTarget.ElementArrayBuffer,
            BufferType.Vertex => BufferTarget.ArrayBuffer,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        fixed (void* d = data)
        {
            GL.BufferData(bufferTarget, data.Length * sizeof(T), d, bufferUsage);
        }
    }

    private void Bind()
    {
        switch (BufferType)
        {
            case BufferType.Index:
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, Handle);
                break;
            case BufferType.Vertex:
                GL.BindBuffer(BufferTarget.ArrayBuffer, Handle);
                break;
        }
    }

    public void Dispose()
    {
        GL.DeleteBuffer(Handle);
    }
}