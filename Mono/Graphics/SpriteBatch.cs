using System.Drawing;
using System.Runtime.InteropServices;
using Mono.Storage;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using SDL;
using static SDL.SDL3;

namespace Mono.Graphics;

[StructLayout(LayoutKind.Sequential)]
public struct SpriteInstance
{
    public Matrix4 Model;
    public Vector4 Color;
    public Vector4 UvRect;
    public Vector2 GlyphSize;
    public float DistanceRange;
    public float IsText;
}

public unsafe class SpriteBatch : IDisposable
{
    private const int MAX_INSTANCE_COUNT = 4096;

    private int _vao;
    private int _quadVbo;
    private int _instanceVbo;
    private int _ebo;

    private readonly Shader _shader;
    private readonly List<SpriteInstance> _spriteBatch = new();
    private Texture? _lastBoundTexture = null;
    private int _batchMode = -1;
    private Matrix4 _view;
    private Matrix4 _projection;

    private GraphicsDevice _graphicsDevice;
    
    internal int DrawCallCount = 0;
    internal int BatchCount = 0;

    private bool _began = false;

    private static readonly float[] SpriteVertices =
    {
        0f, 0f,
        1f, 0f,
        0f, 1f,
        1f, 1f
    };

    private static readonly uint[] Indices =
    {
        0, 1, 2,
        1, 3, 2
    };
    
    public SpriteBatch(GraphicsDevice graphicsDevice, TitleStorage storage)
    {
        _graphicsDevice = graphicsDevice;
        
        InitializeBatchRenderer();

        _shader = new Shader(storage, "Assets/Shaders/batch.vert", "Assets/Shaders/batch.frag");
        _shader.Use();
        _shader.SetInt("uTexture", 0);

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
    }
    
    public void Begin(Matrix4 projection, Matrix4 view)
    {
        if (_began) throw new Exception("Tryna start while there is an active batch huh?");
        _began = true;
        _projection = projection;
        _view = view;
        _spriteBatch.Clear();
    }

    public void End()
    {
        if (!_began) throw new Exception("Tryna end without beginning the batch huh?");
        _began = false;
        if (_spriteBatch.Count == 0) return;
        Flush();
    }

    private void PushInstance(SpriteInstance instance)
    {
        if (!_began) throw new Exception("Tryna draw without beginning the batch huh?");
        
        DrawCallCount += 1;
        if (_spriteBatch.Count >= MAX_INSTANCE_COUNT)
        {
            Flush();
        }

        _spriteBatch.Add(instance);
    }

    public void Draw(RenderTarget renderTarget, RectangleF rect, Color4<Rgba> color, float rotation, Vector2 origin)
    {
        Draw(renderTarget.Texture, rect, color, rotation, origin, new Vector4(0, 1, 1, 0));
    }

    public void Draw(Texture texture, RectangleF rect, Color4<Rgba> color, float rotation, Vector2 origin)
    {
        Draw(texture, rect, color, rotation, origin, new Vector4(0, 0, 1, 1));
    }

    public void Draw(Texture texture, RectangleF rect, Color4<Rgba> color, float rotation, Vector2 origin, Vector4 uvRect)
    {
        if (_lastBoundTexture != texture || _batchMode != 0)
        {
            if (_spriteBatch.Count > 0)
                Flush();

            _batchMode = 0;
            _lastBoundTexture = texture;
        }

        var instance = CreateSpriteInstance(new Vector4(rect.X, rect.Y, rect.Width, rect.Height),
            new Vector4(color.X, color.Y, color.Z, color.W), rotation, origin, uvRect);

        PushInstance(instance);
    }
    
    public void DrawText(Font font, string text, Vector2 position, float lineHeight, Color4<Rgba> color)
    {
        if (_lastBoundTexture != font.Atlas || _batchMode != 1)
        {
            if (_spriteBatch.Count > 0)
                Flush();

            _batchMode = 1;
            _lastBoundTexture = font.Atlas;
        }

        float scale = lineHeight / (font.LineHeight * font.EmSize);
        float x = position.X;
        float baseline = position.Y + font.Ascender * font.EmSize * scale;
        uint prev = 0;

        foreach (char c in text)
        {
            if (c == '\n')
            {
                x = position.X;
                baseline += font.LineHeight * font.EmSize * scale;
                prev = 0;
                continue;
            }

            if (!font.Glyphs.TryGetValue(c, out Font.Glyph glyph))
                continue;

            float kern = 0.0f;
            if (prev != 0) kern = font.GetKerning((uint)prev, c) * scale;

            if (glyph.HasGeometry)
            {
                var glyphPos = new Vector2(
                    x + glyph.XOffset * font.EmSize * scale,
                    baseline - glyph.YOffset * font.EmSize * scale - glyph.Height * scale
                );
                var glyphSize = new Vector2(glyph.Width * scale, glyph.Height * scale);

                SpriteInstance instance = new SpriteInstance();

                instance.Model = Matrix4.CreateScale(new Vector3(glyphSize.X, glyphSize.Y, 1.0f))
                                 * Matrix4.CreateTranslation(new Vector3(glyphPos.X, glyphPos.Y, 0));

                instance.Color = new Vector4(color.X, color.Y, color.Z, color.W);
                instance.UvRect = new Vector4(glyph.U0, glyph.V0, glyph.U1, glyph.V1);
                instance.GlyphSize = glyphSize;
                instance.DistanceRange = font.DistanceRange;
                instance.IsText = 1.0f;

                PushInstance(instance);
            }

            x += glyph.XAdvance * font.EmSize * scale + kern;
            prev = c;
        }
    }

    public void DrawText(
        Font font,
        string text,
        Vector2 position,
        float lineHeight,
        Color4<Rgba> color,
        float maxWidth)
    {
        if (_lastBoundTexture != font.Atlas || _batchMode != 1)
        {
            if (_spriteBatch.Count > 0)
                Flush();

            _batchMode = 1;
            _lastBoundTexture = font.Atlas;
        }

        float scale = lineHeight / (font.LineHeight * font.EmSize);
        float pixelLineHeight = lineHeight; // = font.LineHeight * font.EmSize * scale

        // Starting position
        float x = position.X;
        float y = position.Y;
        float lineStart = position.X;
        float baseline = y + font.Ascender * font.EmSize * scale;

        uint prev = 0;

        // Split into paragraphs (newlines)
        string[] lines = text.Split('\n');

        for (int lineIdx = 0; lineIdx < lines.Length; lineIdx++)
        {
            string line = lines[lineIdx];

            // If it's not the first line, advance to next line
            if (lineIdx > 0)
            {
                x = lineStart;
                baseline += pixelLineHeight;
                prev = 0;
            }

            // Word-wrap processing
            string[] words = line.Split(' ');

            for (int wordIdx = 0; wordIdx < words.Length; wordIdx++)
            {
                string word = words[wordIdx];

                // Empty string from consecutive spaces → advance by space width
                if (string.IsNullOrEmpty(word))
                {
                    if (font.Glyphs.TryGetValue(' ', out var spaceGlyph))
                        x += spaceGlyph.XAdvance * font.EmSize * scale;
                    continue;
                }

                // Measure word width (including kerning from current prev)
                float wordWidth = Font.MeasureWord(font, word, scale);

                // Word wrap: if not at beginning of line and doesn't fit, move to next line
                if (x > lineStart && x + wordWidth > lineStart + maxWidth)
                {
                    x = lineStart;
                    baseline += pixelLineHeight;
                    prev = 0; // no kerning after line break
                }

                // Draw the word
                foreach (char c in word)
                {
                    if (!font.Glyphs.TryGetValue(c, out Font.Glyph glyph))
                        continue;

                    // Apply kerning
                    float kern = prev != 0 ? font.GetKerning(prev, c) * scale : 0f;
                    x += kern;

                    if (glyph.HasGeometry)
                    {
                        Vector2 glyphPos = new Vector2(
                            x + glyph.XOffset * font.EmSize * scale,
                            baseline - glyph.YOffset * font.EmSize * scale - glyph.Height * scale
                        );

                        Vector2 glyphSize = new Vector2(
                            glyph.Width * scale,
                            glyph.Height * scale
                        );

                        SpriteInstance instance = new SpriteInstance
                        {
                            Model =
                                Matrix4.CreateScale(new Vector3(glyphSize.X, glyphSize.Y, 1)) *
                                Matrix4.CreateTranslation(new Vector3(glyphPos.X, glyphPos.Y, 0)),

                            Color = new Vector4(color.X, color.Y, color.Z, color.W),
                            UvRect = new Vector4(glyph.U0, glyph.V0, glyph.U1, glyph.V1),
                            GlyphSize = glyphSize,
                            DistanceRange = font.DistanceRange,
                            IsText = 1.0f
                        };

                        PushInstance(instance);
                    }

                    x += glyph.XAdvance * font.EmSize * scale;
                    prev = c;
                }

                // Space after a word, except after the last word of the line
                if (wordIdx < words.Length - 1 || line.EndsWith(' '))
                {
                    if (font.Glyphs.TryGetValue(' ', out var spaceGlyph))
                        x += spaceGlyph.XAdvance * font.EmSize * scale;
                }
            }

            // After processing a line, update prev for kerning across words on next line?
            // No, kerning is reset to 0 at line breaks, so we already set prev = 0 when moving down.
            // But here, after finishing a line, the next word (if lineIdx+1) will start with prev=0
        }
    }

    private unsafe void InitializeBatchRenderer()
    {
        _vao = GL.GenVertexArray();
        _quadVbo = GL.GenBuffer();
        _instanceVbo = GL.GenBuffer();
        _ebo = GL.GenBuffer();

        GL.BindVertexArray(_vao);

        // Quad vertices
        GL.BindBuffer(BufferTarget.ArrayBuffer, _quadVbo);
        GL.BufferData(
            BufferTarget.ArrayBuffer,
            SpriteVertices.Length * sizeof(float),
            SpriteVertices,
            BufferUsage.StaticDraw);

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(
            0,
            2,
            VertexAttribPointerType.Float,
            false,
            2 * sizeof(float),
            0);

        // Indices
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(
            BufferTarget.ElementArrayBuffer,
            Indices.Length * sizeof(uint),
            Indices,
            BufferUsage.StaticDraw);

        // Instance buffer
        GL.BindBuffer(BufferTarget.ArrayBuffer, _instanceVbo);

        GL.BufferData(
            BufferTarget.ArrayBuffer,
            MAX_INSTANCE_COUNT * Marshal.SizeOf<SpriteInstance>(),
            IntPtr.Zero,
            BufferUsage.StreamDraw);

        int stride = Marshal.SizeOf<SpriteInstance>();

        // Model matrix (locations 2-5)
        int modelOffset = Marshal.OffsetOf<SpriteInstance>(
            nameof(SpriteInstance.Model)).ToInt32();

        for (uint i = 0; i < 4; i++)
        {
            GL.EnableVertexAttribArray(2 + i);

            GL.VertexAttribPointer(
                2 + i,
                4,
                VertexAttribPointerType.Float,
                false,
                stride,
                (void*)(modelOffset + i * Vector4.SizeInBytes)
            );

            GL.VertexAttribDivisor(2 + i, 1);
        }

        int colorOffset = Marshal.OffsetOf<SpriteInstance>(
            nameof(SpriteInstance.Color)).ToInt32();

        GL.EnableVertexAttribArray(6);
        GL.VertexAttribPointer(
            6,
            4,
            VertexAttribPointerType.Float,
            false,
            stride,
            colorOffset);

        GL.VertexAttribDivisor(6, 1);


        int uvOffset = Marshal.OffsetOf<SpriteInstance>(
            nameof(SpriteInstance.UvRect)).ToInt32();

        GL.EnableVertexAttribArray(7);
        GL.VertexAttribPointer(
            7,
            4,
            VertexAttribPointerType.Float,
            false,
            stride,
            uvOffset);

        GL.VertexAttribDivisor(7, 1);


        int glyphOffset = Marshal.OffsetOf<SpriteInstance>(
            nameof(SpriteInstance.GlyphSize)).ToInt32();

        GL.EnableVertexAttribArray(8);
        GL.VertexAttribPointer(
            8,
            2,
            VertexAttribPointerType.Float,
            false,
            stride,
            glyphOffset);

        GL.VertexAttribDivisor(8, 1);


        int distanceOffset = Marshal.OffsetOf<SpriteInstance>(
            nameof(SpriteInstance.DistanceRange)).ToInt32();

        GL.EnableVertexAttribArray(9);
        GL.VertexAttribPointer(
            9,
            1,
            VertexAttribPointerType.Float,
            false,
            stride,
            distanceOffset);

        GL.VertexAttribDivisor(9, 1);


        int textOffset = Marshal.OffsetOf<SpriteInstance>(
            nameof(SpriteInstance.IsText)).ToInt32();

        GL.EnableVertexAttribArray(10);
        GL.VertexAttribPointer(
            10,
            1,
            VertexAttribPointerType.Float,
            false,
            stride,
            textOffset);

        GL.VertexAttribDivisor(10, 1);

        GL.BindVertexArray(0);
    }

    private SpriteInstance CreateSpriteInstance(
        Vector4 rect,
        Vector4 color,
        float rotation,
        Vector2 origin,
        Vector4 uvRect = default)
    {
        if (uvRect == default)
            uvRect = new Vector4(0, 0, 1, 1);

        Vector2 position = new(rect.X, rect.Y);
        Vector2 size = new(rect.Z, rect.W);

        Vector2 pivot = origin * size;

        Matrix4 model =
            Matrix4.CreateScale(size.X, size.Y, 1f)
            * Matrix4.CreateTranslation(-pivot.X, -pivot.Y, 0f)
            * Matrix4.CreateRotationZ(
                MathHelper.DegreesToRadians(rotation))
            * Matrix4.CreateTranslation(position.X, position.Y, 0f);

        return new SpriteInstance
        {
            Model = model,
            Color = color,
            UvRect = uvRect,
            GlyphSize = Vector2.Zero,
            DistanceRange = 0f,
            IsText = 0f
        };
    }

    public void Flush()
    {
        if (_spriteBatch.Count == 0)
            return;

        BatchCount += 1;
        int size = _spriteBatch.Count * Marshal.SizeOf<SpriteInstance>();

        GL.BindBuffer(BufferTarget.ArrayBuffer, _instanceVbo);

        // orphan the old storage
        GL.BufferData(
            BufferTarget.ArrayBuffer,
            MAX_INSTANCE_COUNT * Marshal.SizeOf<SpriteInstance>(),
            IntPtr.Zero,
            BufferUsage.StaticDraw);

        // upload only the used bytes
        GL.BufferSubData(
            BufferTarget.ArrayBuffer,
            IntPtr.Zero,
            size,
            CollectionsMarshal.AsSpan(_spriteBatch));

        GL.BindVertexArray(_vao);

        _shader.SetMatrix4("projection", _projection);
        _shader.SetMatrix4("view", _view);
        
        _lastBoundTexture?.Bind();

        GL.DrawElementsInstanced(
            PrimitiveType.Triangles,
            6,
            DrawElementsType.UnsignedInt,
            IntPtr.Zero,
            _spriteBatch.Count);

        GL.BindVertexArray(0);

        _spriteBatch.Clear();
    }
    
    public Matrix4 MakeDefault2DProjection()
    {
        if (_graphicsDevice.RenderTarget != null)
        {
            return Matrix4.CreateOrthographicOffCenter(0, _graphicsDevice.RenderTarget.Width, 
                _graphicsDevice.RenderTarget.Height, 0, -1.0f, 1.0f);
        }

        float w, h;
        w = _graphicsDevice.FramebufferSize.X;
        h = _graphicsDevice.FramebufferSize.Y;
        return Matrix4.CreateOrthographicOffCenter(0, w, h, 0, -1.0f, 1.0f);
    }
    
    public Matrix4 Make2DProjection(Vector2 size)
    {
        return Matrix4.CreateOrthographicOffCenter(0, size.X, size.Y, 0, -1.0f, 1.0f);
    }

    public void Dispose()
    {
        _shader.Dispose();
    }
}