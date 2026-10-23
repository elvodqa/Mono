using Mono.Storage;
using OpenTK.Mathematics;

namespace Mono.Graphics;

public struct MaterialProperties
{
    // Colors
    public Vector4 Albedo;
    public Vector3 Emission;
    
    // PBR parameters
    public float Metallic;
    public float Roughness;
    public float Ao;
    
    // UV settings
    public Vector2 UvScale;
    public Vector2 UvOffset;
    
    // Render state
    public bool DoubleSided;
    public bool Transparent;
    public bool CastShadows;
}


public enum TextureSlot
{
    Albedo,
    Normal,
    Metallic,
    Roughness,
    AO,
    Emission,
}

public class Material
{
    public Shader Shader;

    public MaterialProperties Properties;

    private Texture[] Textures = new Texture[Enum.GetValues<TextureSlot>().Length];
    public Texture this[TextureSlot slot]
    {
        get => Textures[(int)slot];
        set => Textures[(int)slot] = value;
    }

    public static Material MakeDefault(TitleStorage titleStorage)
    {
        Material material = new Material();
        material.Shader = new Shader(titleStorage, "Assets/Shaders/default_3d.vert", "Assets/Shaders/default_3d.frag");
        material.Properties = new MaterialProperties
        {
            Albedo = Vector4.One,
            Emission = Vector3.Zero,

            Metallic = 0.0f,
            Roughness = 1.0f,
            Ao = 1.0f,

            UvScale = Vector2.One,
            UvOffset = Vector2.Zero,

            DoubleSided = false,
            Transparent = false,
            CastShadows = true,
        };
        
        material[TextureSlot.Albedo]    = Texture.White;
        material[TextureSlot.Normal]    = Texture.FlatNormal;
        material[TextureSlot.Metallic]  = Texture.Black;
        material[TextureSlot.Roughness] = Texture.White;
        material[TextureSlot.AO]        = Texture.White;
        material[TextureSlot.Emission]  = Texture.Black;
        
        return material;
    }
}