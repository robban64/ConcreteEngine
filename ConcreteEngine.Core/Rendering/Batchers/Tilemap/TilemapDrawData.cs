#region

using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering;

public readonly struct TileDrawItem(ushort atlasX, ushort atlasY)
{
    public readonly ushort AtlasX = atlasX;
    public readonly ushort AtlasY = atlasY;
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