using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.Scene.Data;

public struct AnimationComponentDesc
{
    public AssetRef<Model> Asset;
    public float Time;
    public float Duration;
    public float Speed;
    public short Clip;
}

public struct SourceComponentDesc
{
    public AssetRef<Model> Asset;
    public int DrawCount;
    public MaterialTagKey MaterialKey;
}
