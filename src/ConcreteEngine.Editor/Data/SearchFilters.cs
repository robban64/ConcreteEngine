using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;

namespace ConcreteEngine.Editor.Data;

public readonly ref struct SearchStringPacked(ReadOnlySpan<char> searchString, ulong searchKey, ulong searchMask)
{
    public readonly ReadOnlySpan<char> SearchString = searchString;
    public readonly ulong SearchKey = searchKey;
    public readonly ulong SearchMask = searchMask;
}

public readonly struct SceneObjectFilter(SceneObjectKind kind, bool? enabled = null)
{
    public readonly bool? Enabled = enabled;
    public readonly SceneObjectKind Kind = kind;
}

public readonly struct SearchAssetFilter(AssetKind kind, bool? enabled = null)
{
    public readonly bool? Enabled = enabled;
    public readonly AssetKind Kind = kind;
}