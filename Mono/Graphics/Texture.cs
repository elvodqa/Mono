using Mono.Storage;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using StbImageSharp;

namespace Mono.Graphics;


public enum WrapMode
{
    Repeat,
    MirroredRepeat,
    ClampToEdge,
    ClampToBorder
}

public enum FilterMode
{
    Nearest,
    Linear
}

public unsafe class Texture : IDisposable
{
    private readonly FilterMode _filterMode;
    private readonly ImageResult _result;
    private readonly WrapMode _wrapMode;
    
    public int Handle;
    public int Width { get; }
    public int Height { get; }
    
    public static Texture White => new(new(1, 1), [Color4.White]);
    public static Texture Black => new(new(1, 1), [Color4.Black]);
    public static Texture FlatNormal => new(new(1, 1), [new Color4<Rgba>(0.5f, 0.5f, 1, 1)]);
    
    public Texture(
        Vector2i size,
        FilterMode filterMode = FilterMode.Linear,
        WrapMode wrapMode = WrapMode.Repeat,
        bool generateMipmaps = true)
    {
        _filterMode = filterMode;
        _wrapMode = wrapMode;

        Width = size.X;
        Height = size.Y;

        Handle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, Handle);

        GL.TexImage2D(
            TextureTarget.Texture2D,
            0,
            InternalFormat.Rgba8,
            Width,
            Height,
            0,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            IntPtr.Zero);

        var wrap = ToGL(_wrapMode);

        GL.TexParameteri(TextureTarget.Texture2D,
            TextureParameterName.TextureWrapS, (int)wrap);
        GL.TexParameteri(TextureTarget.Texture2D,
            TextureParameterName.TextureWrapT, (int)wrap);

        if (generateMipmaps)
        {
            GL.TexParameteri(TextureTarget.Texture2D,
                TextureParameterName.TextureMinFilter,
                (int)(_filterMode == FilterMode.Linear
                    ? TextureMinFilter.LinearMipmapLinear
                    : TextureMinFilter.NearestMipmapNearest));
        }
        else
        {
            GL.TexParameteri(TextureTarget.Texture2D,
                TextureParameterName.TextureMinFilter,
                (int)(_filterMode == FilterMode.Linear
                    ? TextureMinFilter.Linear
                    : TextureMinFilter.Nearest));
        }

        GL.TexParameteri(TextureTarget.Texture2D,
            TextureParameterName.TextureMagFilter,
            (int)(_filterMode == FilterMode.Linear
                ? TextureMagFilter.Linear
                : TextureMagFilter.Nearest));

        if (generateMipmaps)
            GL.GenerateMipmap(TextureTarget.Texture2D);

        GL.BindTexture(TextureTarget.Texture2D, 0);
    }
    
    public Texture(TitleStorage storage, string path, FilterMode filterMode = FilterMode.Linear, WrapMode wrapMode = WrapMode.Repeat,
        bool generateMipmaps = true)
    {
        _filterMode = filterMode;
        _wrapMode = wrapMode;
        StbImage.stbi_set_flip_vertically_on_load(0);

        if (!storage.Exists(path)) throw new Exception($"Texture not found in path: {path}");

        storage.GetFileSize(path, out var size);

        Span<byte> imageData = new Span<byte>(new byte[size]);
        storage.ReadFile(path, imageData);
        _result = ImageResult.FromMemory(imageData.ToArray(), ColorComponents.RedGreenBlueAlpha);

        Width = _result.Width;
        Height = _result.Height;

        Handle = GL.GenTexture();
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, Handle);

        fixed (byte* ptr = _result.Data)
        {
            var format = _result.Comp == ColorComponents.RedGreenBlueAlpha
                ? PixelFormat.Rgba
                : PixelFormat.Rgb;

            var internalFormat = _result.Comp == ColorComponents.RedGreenBlueAlpha
                ? InternalFormat.Rgba
                : InternalFormat.Rgb;

            GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat,
                _result.Width, _result.Height, 0, format, PixelType.UnsignedByte, ptr);
        }

        GL.BindTexture(TextureTarget.Texture2D, Handle);

        var wrap = ToGL(_wrapMode);

        GL.TexParameteri(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)wrap);
        GL.TexParameteri(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)wrap);

        // Filter
        if (generateMipmaps)
        {
            var mipmapFilter = _filterMode == FilterMode.Linear
                ? TextureMinFilter.LinearMipmapLinear
                : TextureMinFilter.NearestMipmapNearest;
            GL.TexParameteri(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)mipmapFilter);
        }
        else
        {
            var filter = _filterMode == FilterMode.Linear
                ? TextureMinFilter.Linear
                : TextureMinFilter.Nearest;
            GL.TexParameteri(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)filter);
        }

        var magFilter = _filterMode == FilterMode.Linear
            ? TextureMagFilter.Linear
            : TextureMagFilter.Nearest;
        GL.TexParameteri(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magFilter);

        if (generateMipmaps)
            GL.GenerateMipmap(TextureTarget.Texture2D);

        GL.BindTexture(TextureTarget.Texture2D, 0);
    }
    
    public Texture(
        Vector2i size,
        ReadOnlySpan<Color4<Rgba>> pixels,
        FilterMode filterMode = FilterMode.Linear,
        WrapMode wrapMode = WrapMode.Repeat,
        bool generateMipmaps = true)
    {
        if (pixels.Length != size.X * size.Y)
            throw new ArgumentException("Pixel count does not match texture size.");

        _filterMode = filterMode;
        _wrapMode = wrapMode;

        Width = size.X;
        Height = size.Y;

        Handle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, Handle);

        fixed (Color4<Rgba>* ptr = pixels)
        {
            GL.TexImage2D(
                TextureTarget.Texture2D,
                0,
                InternalFormat.Rgba32f,
                Width,
                Height,
                0,
                PixelFormat.Rgba,
                PixelType.Float,
                ptr);
        }

        var wrap = ToGL(_wrapMode);

        GL.TexParameteri(TextureTarget.Texture2D,
            TextureParameterName.TextureWrapS, (int)wrap);
        GL.TexParameteri(TextureTarget.Texture2D,
            TextureParameterName.TextureWrapT, (int)wrap);

        GL.TexParameteri(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMinFilter,
            (int)(generateMipmaps
                ? (_filterMode == FilterMode.Linear
                    ? TextureMinFilter.LinearMipmapLinear
                    : TextureMinFilter.NearestMipmapNearest)
                : (_filterMode == FilterMode.Linear
                    ? TextureMinFilter.Linear
                    : TextureMinFilter.Nearest)));

        GL.TexParameteri(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMagFilter,
            (int)(_filterMode == FilterMode.Linear
                ? TextureMagFilter.Linear
                : TextureMagFilter.Nearest));

        if (generateMipmaps)
            GL.GenerateMipmap(TextureTarget.Texture2D);

        GL.BindTexture(TextureTarget.Texture2D, 0);
    }
    
    private static TextureWrapMode ToGL(WrapMode mode)
    {
        return mode switch
        {
            WrapMode.Repeat => TextureWrapMode.Repeat,
            WrapMode.MirroredRepeat => TextureWrapMode.MirroredRepeat,
            WrapMode.ClampToEdge => TextureWrapMode.ClampToEdge,
            WrapMode.ClampToBorder => TextureWrapMode.ClampToBorder,
            _ => TextureWrapMode.Repeat
        };
    }
    
    public void Bind(TextureUnit unit = TextureUnit.Texture0)
    {
        GL.ActiveTexture(unit);
        GL.BindTexture(TextureTarget.Texture2D, Handle);
    }

    public void Unbind()
    {
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }
    
    public IntPtr ToIntPtr()
    {
        return Handle;
    }
    
     public void Dispose()
    {
        GL.DeleteTexture(Handle);
    }
}