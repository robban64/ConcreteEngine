using System.Runtime.CompilerServices;

namespace ConcreteEngine.Renderer.Buffer;

public sealed class RenderUploadBuffers : IDisposable
{
    public readonly DrawCommandBuffer Commands = new();
    public readonly MaterialBuffer Materials = new();
    public readonly SkinningBuffer Skinning = new();
    public readonly EffectBuffer Effects = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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