using System.Runtime.InteropServices;
using OpenTK.Mathematics;

namespace Mono.Graphics;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex
{
    public Vector3 Position;
    public Vector3 Normal;
    public Vector2 UV;
    public Vector4 Tangent; // bittangent stored in w (1, -1)
}