using ConcreteEngine.Core.Rendering.SpriteBatching;
using ConcreteEngine.Graphics;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Rendering.Tilemap;

public readonly struct TileData(ushort atlasX, ushort atlasY)
{
    public readonly ushort AtlasX = atlasX;
    public readonly ushort AtlasY = atlasY;
}

public readonly struct TilemapDrawData(
    Vector2D<float> position,
    Vector2D<float> scale,
    Vector2D<float> textureScale)
{
    public readonly Vector2D<float> Position = position;
    public readonly Vector2D<float> Scale = scale;
    public readonly Vector2D<float> TextureScale = textureScale;
}

public readonly struct TileChunkBuildResult(ushort meshId, uint drawCount)
{
    public readonly ushort MeshId = meshId;
    public readonly uint DrawCount = drawCount;
}

public readonly struct TilemapBatchResult(in TileChunkBuildResult groundLayer)
{
    public readonly TileChunkBuildResult GroundLayer = groundLayer;
}
