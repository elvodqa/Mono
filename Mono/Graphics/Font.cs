using System.Text;
using System.Text.Json;
using Mono.Storage;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using StbImageSharp;

namespace Mono.Graphics;

public struct TextBounds
{
    public float Width;
    public float Height;
    public int Lines;

    public TextBounds(float width, float height, int lines)
    {
        Width = width;
        Height = height;
        Lines = lines;
    }
}

public class Font
{
    public float Ascender;
    public Texture Atlas;
    public float Descender;
    public float DistanceRange;
    public float EmSize;

    public Dictionary<uint, Glyph> Glyphs = new();
    public Dictionary<(uint, uint), float> Kerning = new();
    public float LineHeight;

    public static Font Load(TitleStorage storage, string atlasPath, string jsonPath)
    {
        Font font = new();
        font.Atlas = new Texture(storage, atlasPath);

        storage.GetFileSize(jsonPath, out var vertexSize);
        var jsonData = new Span<byte>(new byte[vertexSize]);
        storage.ReadFile(jsonPath, jsonData);

        LoadJson(font, Encoding.UTF8.GetString(jsonData));
        return font;
    }

    private static void LoadJson(Font font, string jsonText)
    {
        var doc = JsonDocument.Parse(jsonText);
        var root = doc.RootElement;
        var atlas = root.GetProperty("atlas");
        font.DistanceRange = atlas.GetProperty("distanceRange").GetSingle();
        font.EmSize = atlas.GetProperty("size").GetSingle();
        var metrics = root.GetProperty("metrics");
        font.LineHeight = metrics.GetProperty("lineHeight").GetSingle();
        font.Ascender = metrics.GetProperty("ascender").GetSingle();
        font.Descender = metrics.GetProperty("descender").GetSingle();
        foreach (var glyphJson in root.GetProperty("glyphs").EnumerateArray())
        {
            var unicode = glyphJson.GetProperty("unicode").GetUInt32();
            Glyph glyph = new();
            glyph.XAdvance = glyphJson.GetProperty("advance").GetSingle();
            if (glyphJson.TryGetProperty("atlasBounds", out var atlasBounds) &&
                glyphJson.TryGetProperty("planeBounds", out var planeBounds))
            {
                glyph.HasGeometry = true;
                glyph.X = atlasBounds.GetProperty("left").GetSingle();
                glyph.Y = atlasBounds.GetProperty("bottom").GetSingle();
                var right = atlasBounds.GetProperty("right").GetSingle();
                var top = atlasBounds.GetProperty("top").GetSingle();
                glyph.Width = right - glyph.X;
                glyph.Height = top - glyph.Y;
                glyph.XOffset = planeBounds.GetProperty("left").GetSingle();
                glyph.YOffset = planeBounds.GetProperty("bottom").GetSingle();
                glyph.U0 = glyph.X / font.Atlas.Width;
                glyph.V0 = 1f - (glyph.Y + glyph.Height) / font.Atlas.Height;
                glyph.U1 = (glyph.X + glyph.Width) / font.Atlas.Width;
                glyph.V1 = 1f - glyph.Y / font.Atlas.Height;
            }

            font.Glyphs[unicode] = glyph;
        }

        if (root.TryGetProperty("kerning", out var kernings))
            foreach (var k in kernings.EnumerateArray())
            {
                var first = k.GetProperty("unicode1").GetUInt32();
                var second = k.GetProperty("unicode2").GetUInt32();
                var advance = k.GetProperty("advance").GetSingle();
                font.Kerning[(first, second)] = advance;
            }
    }

    public float GetKerning(uint first, uint second)
    {
        if (Kerning.TryGetValue((first, second), out var k)) return k;
        return 0f;
    }

    public static TextBounds MeasureText(
        Font font,
        string text,
        float pixelLineHeight,
        float maxWidth = float.MaxValue)
    {
        float scale = pixelLineHeight / (font.LineHeight * font.EmSize);

        // Helper to get the advance of a single character including kerning
        float GetAdvance(uint c, uint prevChar)
        {
            if (!font.Glyphs.TryGetValue(c, out Glyph glyph))
                return 0.0f;

            float kern = 0.0f;
            if (prevChar != 0)
                font.Kerning.TryGetValue((prevChar, c), out kern);

            return (glyph.XAdvance * font.EmSize + kern) * scale;
        }

        float lineHeight = font.LineHeight * font.EmSize * scale;

        float x = 0.0f;
        float maxX = 0.0f;
        int lines = 1;
        uint prev = 0;

        // NOTE: This iterates over .NET chars (UTF-16). For full Unicode support,
        // you should extract codepoints (e.g., using System.Globalization.StringInfo).
        // The original C++ code cast each char to uint32_t, which only handles ASCII
        // correctly. If your text is ASCII, this is equivalent.
        foreach (char ch in text)
        {
            uint c = (uint)ch; // same as the original (uint32_t)text[i]

            // Newline handling
            if (c == '\n')
            {
                maxX = Math.Max(maxX, x);
                x = 0.0f;
                prev = 0;
                lines++;
                continue;
            }

            float advance = GetAdvance(c, prev);

            // Word wrap (if maxWidth is set)
            if (maxWidth > 0 && x != 0 && x + advance > maxWidth)
            {
                maxX = Math.Max(maxX, x);
                x = 0.0f;
                prev = 0;
                lines++;
            }

            x += advance;
            prev = c;
            maxX = Math.Max(maxX, x);
        }

        return new TextBounds(maxX, lines * lineHeight, lines);
    }

    public static float MeasureWord(Font font, string word, float scale)
    {
        if (string.IsNullOrEmpty(word)) return 0f;

        float width = 0f;
        uint prev = 0;

        foreach (char c in word)
        {
            if (!font.Glyphs.TryGetValue(c, out var glyph))
                continue;

            float kern = prev != 0 ? font.GetKerning(prev, c) * scale : 0f;
            width += glyph.XAdvance * font.EmSize * scale + kern;
            prev = c;
        }

        return width;
    }

    public struct Glyph
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;
        public float XOffset;
        public float YOffset;
        public float XAdvance;
        public float U0;
        public float V0;
        public float U1;
        public float V1;
        public bool HasGeometry;
    }
}