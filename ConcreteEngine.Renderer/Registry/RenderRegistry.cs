#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Renderer.Passes;

#endregion

namespace ConcreteEngine.Renderer.Registry;

internal sealed class RenderRegistry
{
    internal readonly record struct RegistrationData(bool Enabled, Size2D OutputSize);

    private RegistrationData _registrationData;

    public RenderShaderRegistry ShaderRegistry { get; }

    public RenderUboRegistry UboRegistry { get; }

    public RenderFboRegistry FboRegistry { get; }


    public RenderRegistry(GfxContext gfx)
    {
        ShaderRegistry = new RenderShaderRegistry(gfx);
        UboRegistry = new RenderUboRegistry(gfx);
        FboRegistry = new RenderFboRegistry(gfx);
    }

    public void BeginRegistration(Size2D outputSize)
    {
        InvalidOpThrower.ThrowIf(_registrationData.Enabled);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outputSize.Width, 1, nameof(outputSize));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outputSize.Height, 1, nameof(outputSize));

        UboRegistry.BeginRegistration();
        FboRegistry.BeginRegistration();
        //FboRegistry.RegisterTemp();
    }

    public void FinishRegistration()
    {
        UboRegistry.FinishRegistration();
        FboRegistry.FinishRegistration();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderShader GetRenderShader(ShaderId shaderId) => ShaderRegistry.GetRenderShader(shaderId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderUbo GetRenderUbo<TUbo>() where TUbo : unmanaged, IStd140Uniform => UboRegistry.GetRenderUbo<TUbo>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderFbo GetRenderFbo(FboTagKey key) => FboRegistry.GetRenderFbo(key)!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetRenderFbo(FboTagKey key, out RenderFbo? fbo) => FboRegistry.TryGetRenderFbo(key, out fbo);
}