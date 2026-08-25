using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Extensions;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Engine.Graphics.Visuals;

public sealed class VisualManager
{
    public static readonly VisualManager Instance = new();

    public bool AnyWasDirty { get; private set; }

    public readonly LightingSettings Lightning;
    public readonly EnvironmentSettings Environment;
    public readonly PostEffectSettings PostEffect;

    private VisualManager()
    {
        if (Instance != null)
            throw new InvalidOperationException($"{nameof(VisualManager)} is already initialized");

        Lightning = new LightingSettings();
        Environment = new EnvironmentSettings();
        PostEffect = new PostEffectSettings();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool CommitShadowSize()
    {
        var hasPendingShadowSize = Lightning.Shadow.HasPendingShadowSize;
        if (hasPendingShadowSize) Lightning.Shadow.HasPendingShadowSize = false;
        return hasPendingShadowSize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ClearWasDirty() => AnyWasDirty = false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Commit()
    {
        var anyWasDirty = AnyWasDirty = false;
        anyWasDirty |= Lightning.Commit();
        anyWasDirty |= Environment.FogSettings.Commit();
        anyWasDirty |= PostEffect.Commit();
        return AnyWasDirty = anyWasDirty;
    }
}

public abstract class VisualSettings
{
    public long Version { get; private set; } = 1;
    public bool IsDirty { get; protected set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool Commit()
    {
        if (!IsDirty) return false;
        IsDirty = false;
        ++Version;
        return true;
    }

    protected float Set(float field, float value)
    {
        if (FloatMath.NearlyEqual(field, value)) return field;
        IsDirty = true;
        return value;
    }
    protected Vector2 Set(Vector2 field, Vector2 value)
    {
        if (VectorMath.NearlyEqual(field, value)) return field;
        IsDirty = true;
        return value;
    }

    protected Vector3 Set(Vector3 field, Vector3 value)
    {
        if (VectorMath.NearlyEqual(field.AsVector128(), value.AsVector128())) return field;
        IsDirty = true;
        return value;
    }
    protected Color4 Set(Color4 field, Color4 value)
    {
        if (Color4.NearlyEqual(field, value)) return field;
        IsDirty = true;
        return value;
    }

  
    protected T Set<T>(T field, T value) where T : unmanaged
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return field;
        IsDirty = true;
        return value;
    }
}