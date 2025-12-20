using System.Numerics;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Engine.Worlds.Mesh.Data;

[StructLayout(LayoutKind.Sequential)]
internal struct ParticleInstanceData
{
    public Vector4 PositionSize;
    public Vector4 Color;
}