using ConcreteEngine.Engine.Worlds.Data;

namespace ConcreteEngine.Engine.Assets.Models;

public readonly struct ModelMeshInfo(ModelId model, AnimationId animation, int partCount, int drawCount)
{
    public readonly int DrawCount = drawCount;
    public readonly ModelId Model = model;
    public readonly AnimationId Animation = animation;
    public readonly byte PartCount = (byte)partCount;
}
