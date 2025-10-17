#region

using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Graphics.Primitives;

#endregion

namespace ConcreteEngine.Core.Assets.Meshes;

internal record struct MeshCreationInfo(MeshId MeshId, int DrawCount);

internal sealed record MeshResultPayload(
    MeshDrawProperties Properties,
    List<uint> Indices,
    List<Vertex3D> Vertices,
    IReadOnlyList<VertexAttributeDesc> Attributes,
    AssetFileSpec FileSpec);