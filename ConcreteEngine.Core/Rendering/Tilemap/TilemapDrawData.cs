using System.Numerics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Rendering.Tilemap;

public readonly struct TileData(ushort atlasX, ushort atlasY)
{
    public readonly ushort AtlasX = atlasX;
    public readonly ushort AtlasY = atlasY;
}

public readonly struct TilemapDrawData(
    Vector2 position,
    Vector2 scale,
    Vector2 textureScale)
{
    public readonly Vector2 Position = position;
    public readonly Vector2 Scale = scale;
    public readonly Vector2 TextureScale = textureScale;
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
