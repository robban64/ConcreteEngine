using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Graphics.Gfx.Resources;

namespace ConcreteEngine.Engine.Worlds.MeshGeneration.MeshData;


[StructLayout(LayoutKind.Sequential)]
internal struct ParticleInstanceData
{
    public Vector4 PositionSize;
    public Vector4 Color;
}