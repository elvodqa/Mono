using OpenTK.Graphics.OpenGL;

namespace Mono.Graphics;

public unsafe class RenderTarget : IDisposable
{
    public int Width
    {
        get => _width;
        private set => _width = value;
    }

    public int Height
    {
        get => _height;
        private set => _height = value;
    }
    
    public int Handle { get; private set; }
    public Texture Texture { get; private set; }
    private int _width;
    private int _height;

    public RenderTarget(int width, int height)
    {
        _width = width;
        _height = height;
        Handle = GL.GenFramebuffer();
        Create();
    }

    public void Resize(int width, int height)
    {
        _width = Math.Clamp(width, 1, 64000);
        _height = Math.Clamp(height, 1, 64000);
        Texture.Dispose();
        Create();
    }

    private void Create()
    {
        int drawFboId = 0, readFboId = 0;
        int lastTexture = 0;
        GL.GetIntegerv(GetPName.DrawFramebufferBinding, &drawFboId);
        GL.GetIntegerv(GetPName.ReadFramebufferBinding, &readFboId);
        GL.GetIntegerv(GetPName.ActiveTexture, &lastTexture);
        
        Width = _width;
        Height = _height;

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, Handle);
        Texture = new Texture(new(_width, _height), FilterMode.Linear, WrapMode.ClampToEdge);
        Texture.Bind();
       
        GL.BindTexture(TextureTarget.Texture2D, 0);
        
        
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, Texture.Handle, 0);

        GL.DrawBuffer(DrawBufferMode.ColorAttachment0);

        var res = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (res != FramebufferStatus.FramebufferComplete)
        {
            throw new Exception($"Framebuffer error: {res}");
        }
        
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, drawFboId);
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, readFboId);
        GL.BindTexture(TextureTarget.Texture2D, lastTexture);
    }
    
    public void Bind()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, Handle);
    }

    public void Dispose()
    {
        Texture.Dispose();
        GL.DeleteFramebuffer(Handle);
    }
}
