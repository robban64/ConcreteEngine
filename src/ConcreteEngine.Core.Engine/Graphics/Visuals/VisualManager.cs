using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Editor;

namespace ConcreteEngine.Core.Engine.Graphics.Visuals;

public sealed class VisualManager
{
    public static readonly VisualManager Instance = new();

    public bool AnyWasDirty { get; private set; }

    public readonly ShadowSettings Shadow;
    public readonly LightingSettings Illumination;
    public readonly FogSettings Fog;
    public readonly PostEffectSettings PostEffect;

    public bool HasPendingShadowSize => Shadow.HasPendingShadowSize;

    private VisualManager()
    {
        if (Instance != null)
            throw new InvalidOperationException($"{nameof(VisualManager)} is already initialized");

        Shadow = new ShadowSettings();
        Illumination = new LightingSettings();
        Fog = new FogSettings();
        PostEffect = new PostEffectSettings();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool CommitShadowSize()
    {
        var hasPendingShadowSize = Shadow.HasPendingShadowSize;
        if (hasPendingShadowSize) Shadow.HasPendingShadowSize = false;
        return hasPendingShadowSize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Commit()
    {
        AnyWasDirty = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Ensure()
    {
        AnyWasDirty = false;
        AnyWasDirty |= Illumination.Ensure();
        AnyWasDirty |= Shadow.Ensure();
        AnyWasDirty |= Fog.Ensure();
        AnyWasDirty |= PostEffect.Ensure();
        return AnyWasDirty;
    }
}

public abstract class VisualStateObject
{
    public ulong Version { get; private set; }
    public bool WasDirty { get; private set; }

    protected bool IsDirty = true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool Ensure()
    {
        if (!IsDirty && WasDirty)
        {
            WasDirty = false;
        }
        else if (IsDirty && !WasDirty)
        {
            IsDirty = false;
            WasDirty = true;
            Version++;
        }

        return WasDirty;
    }
}

