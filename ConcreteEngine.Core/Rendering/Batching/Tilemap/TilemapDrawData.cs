#region

using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Batching;

public readonly struct TileDrawItem(ushort textureIndex)
{
    public readonly ushort TextureIndex = textureIndex;
}

public readonly struct TileChunkBuildResult(MeshId meshId, uint drawCount)
{
    public readonly MeshId MeshId = meshId;
    public readonly uint DrawCount = drawCount;
}

public readonly struct TilemapBatchResult(in TileChunkBuildResult groundLayer)
{
    public readonly TileChunkBuildResult GroundLayer = groundLayer;
}