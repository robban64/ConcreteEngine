using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;

namespace ConcreteEngine.Editor.Data;

public readonly ref struct SearchPayload<T>(
    ReadOnlySpan<char> searchString,
    Span<T> dst,
    ulong searchKey,
    ulong searchMask)
{
    public readonly Span<T> Destination = dst;
    public readonly ReadOnlySpan<char> SearchString = searchString;
    public readonly ulong SearchKey = searchKey;
    public readonly ulong SearchMask = searchMask;
}

// TODO
public readonly struct SearchAssetFilter(AssetKind kind, int filter)
{
    public readonly int Filter = filter;
    public readonly AssetKind Kind = kind;
}

public readonly struct SearchFilter(byte kind, bool? enabled, int filter)
{
    public readonly int Filter = filter;
    public readonly bool? Enabled = enabled;
    public readonly byte Kind = kind;

    public AssetKind AsAssetKind => (AssetKind)Kind;
    public SceneObjectKind AsSceneKind => (SceneObjectKind)Kind;


    public static SearchFilter MakeScene(SceneObjectKind kind, bool? enabled = null, int filter = 0) =>
        new((byte)kind, enabled, filter);

    public static SearchFilter MakeAsset(AssetKind kind, bool? enabled = null, int filter = 0) =>
        new((byte)kind, enabled, filter);
}