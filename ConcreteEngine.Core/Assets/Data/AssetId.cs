namespace ConcreteEngine.Core.Assets.Data;

public readonly record struct AssetId(int Value)
{
    public bool IsValid => Value > 0;
    public static implicit operator int(AssetId id) => id.Value;

}

public readonly record struct AssetFileId(int Value)
{
    public bool IsValid => Value > 0;
}

public readonly record struct AssetRef<TAsset>(AssetId Id) where TAsset : AssetObject
{
    public int Value => Id.Value;
    public bool IsValid => Id.IsValid;

    public static explicit operator AssetRef<TAsset>(AssetId id) => new(id);
    public static implicit operator AssetId(AssetRef<TAsset> typed) => typed.Id;

    public static AssetRef<TAsset> Make(AssetId id) => new(id);
}

// Needed?
public readonly record struct AssetFileRef<TAsset>(AssetFileId Value) where TAsset : AssetObject
{
    public static explicit operator AssetFileRef<TAsset>(AssetFileId id) => new(id);
    public static implicit operator AssetFileId(AssetFileRef<TAsset> typed) => typed.Value;
}