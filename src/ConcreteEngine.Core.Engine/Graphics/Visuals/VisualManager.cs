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
    public readonly LightingSettings Lightning;
    public readonly FogSettings Fog;
    public readonly PostEffectSettings PostEffect;

    public bool HasPendingShadowSize => Shadow.HasPendingShadowSize;

    private VisualManager()
    {
        if (Instance != null)
            throw new InvalidOperationException($"{nameof(VisualManager)} is already initialized");

        Shadow = new ShadowSettings();
        Lightning = new LightingSettings();
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
        var anyWasDirty = AnyWasDirty = false;
        anyWasDirty |= Lightning.Ensure();
        anyWasDirty |= Shadow.Ensure();
        anyWasDirty |= Fog.Ensure();
        anyWasDirty |= PostEffect.Ensure();
        return AnyWasDirty = anyWasDirty;
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
        bool isDirty = IsDirty, wasDirty = WasDirty;
        if (!isDirty && wasDirty)
        {
            WasDirty = false;
        }
        else if (isDirty && !wasDirty)
        {
            IsDirty = false;
            WasDirty = true;
            ++Version;
        }

        return WasDirty;
    }
    
    
    public static float Set(float field, float value, ref bool isDirty)
    {
        if (FloatMath.NearlyEqual(field, value)) return field;
        isDirty = true;
        return value;
    }

    public static Vector3 Set(Vector3 field, Vector3 value, ref bool isDirty)
    {
        if (VectorMath.NearlyEqual(field, value)) return field;
        isDirty = true;
        return value;
    }

    public static Vector2 Set(Vector2 field, Vector2 value, ref bool isDirty)
    {
        if (VectorMath.NearlyEqual(field, value)) return field;
        isDirty = true;
        return value;
    }

    public static T Set<T>(T field, T value, ref bool isDirty) where T : unmanaged
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return field;
        isDirty = true;
        return value;
    }
}

