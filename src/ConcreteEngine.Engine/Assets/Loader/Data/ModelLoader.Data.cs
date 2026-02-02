using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Assets.Loader.Data;


internal ref struct MeshUploadData<TVertex>(
    ReadOnlySpan<TVertex> vertices,
    ReadOnlySpan<uint> indices) where TVertex : unmanaged
{
    public ReadOnlySpan<TVertex> Vertices = vertices;
    public ReadOnlySpan<uint> Indices = indices;
}