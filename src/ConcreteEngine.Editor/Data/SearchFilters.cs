using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;

namespace ConcreteEngine.Editor.Data;

public readonly ref struct SearchPayload<T>(ReadOnlySpan<char> searchString, Span<T> dst, ulong searchKey, ulong searchMask)
{
    public readonly Span<T> Destination = dst;
    public readonly ReadOnlySpan<char> SearchString = searchString;
    public readonly ulong SearchKey = searchKey;
    public readonly ulong SearchMask = searchMask;
}

public readonly struct SearchFilter(byte kind, bool? enabled)
{
    public readonly bool? Enabled = enabled;
    public readonly byte Kind = kind;

    public AssetKind AsAssetKind => (AssetKind)Kind;
    public SceneObjectKind AsSceneKind => (SceneObjectKind)Kind;


    public static SearchFilter MakeScene(SceneObjectKind kind, bool? enabled = null) => new((byte)kind, enabled);
    public static SearchFilter MakeAsset(AssetKind kind, bool? enabled = null) => new((byte)kind, enabled);

}