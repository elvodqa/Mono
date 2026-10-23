namespace Mono.Graphics;

public class Mesh
{
    public uint Vao;
    public uint Vbo;
    public uint Ebo;

    public uint IndexCount;

    public Material Material;

    public Mesh(ReadOnlySpan<Vertex> vertex, ReadOnlySpan<UInt32> indices, Material material)
    {
        
    }
}