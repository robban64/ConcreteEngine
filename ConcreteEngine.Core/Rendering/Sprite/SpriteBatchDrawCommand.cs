#region

using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Definitions;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Core.Rendering.Sprite;


public readonly struct SpriteDrawCommand : IDrawCommandMessage
{
    public SpriteDrawCommand(ushort meshId, ushort shaderId, ushort textureId, uint drawCount, in Matrix4X4<float> transform)
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
