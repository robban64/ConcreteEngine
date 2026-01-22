using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Core.Engine.Assets;

public interface IShader : IAsset
{
    ShaderId GfxId { get; }
    int Samplers { get; }
}