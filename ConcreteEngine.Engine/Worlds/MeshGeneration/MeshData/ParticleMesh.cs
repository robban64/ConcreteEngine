#region

using System.Numerics;
using System.Runtime.InteropServices;

#endregion

namespace ConcreteEngine.Engine.Worlds.MeshGeneration.MeshData;

[StructLayout(LayoutKind.Sequential)]
internal struct ParticleInstanceData
{
    public Vector4 PositionSize;
    public Vector4 Color;
}