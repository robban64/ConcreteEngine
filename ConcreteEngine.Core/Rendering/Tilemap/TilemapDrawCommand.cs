using Silk.NET.Maths;

namespace ConcreteEngine.Core.Rendering.Tilemap;

public readonly struct TilemapDrawCommand : IDrawCommandMessage
{
    public TilemapDrawCommand(ushort meshId, ushort shaderId, ushort textureId, uint drawCount, in Matrix4X4<float> transform)
    {
        MeshId = meshId;
        ShaderId = shaderId;
        TextureId = textureId;
        DrawCount = drawCount;
        Transform = transform;
    }

    public readonly ushort MeshId;
    public readonly ushort ShaderId ;
    public readonly ushort TextureId;
    public readonly uint DrawCount;

    public readonly Matrix4X4<float> Transform;
}
