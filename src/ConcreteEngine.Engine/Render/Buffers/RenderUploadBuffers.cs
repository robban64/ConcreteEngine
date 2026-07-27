using System.Runtime.CompilerServices;

namespace ConcreteEngine.Engine.Render.Buffers;

internal sealed class RenderUploadBuffers : IDisposable
{
    public readonly DrawBuffer Commands = new();
    public readonly MaterialBuffer Materials = new();
    public readonly SkinningBuffer Skinning = new();
    public readonly EffectBuffer Effects = new();

    internal void Reset()
    {
        Commands.Reset();
        Materials.NewFrame();
        Skinning.Reset();
        Effects.Reset();
    }

    public void Dispose()
    {
        Commands.Dispose();
        Materials.Dispose();
        Skinning.Dispose();
    }
}