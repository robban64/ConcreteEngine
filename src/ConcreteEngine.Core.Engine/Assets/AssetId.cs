namespace ConcreteEngine.Core.Engine.Assets;

public readonly record struct AssetId(int Value)
{
    public bool IsValid() => Value > 0;
    public static implicit operator int(AssetId id) => id.Value;
}

public readonly record struct AssetId<TAsset>(AssetId Id) where TAsset : AssetObject
{
    public bool IsValid() => Id.IsValid();
    public static implicit operator int(AssetId<TAsset> typed) => typed.Id;
    public static implicit operator AssetId(AssetId<TAsset> typed) => typed.Id;
}

public readonly record struct AssetFileId(int Value);

/*
public readonly record struct AssetGuid(Guid Value)
{
    public static explicit operator AssetGuid(Guid value) => new (value);
    public static implicit operator Guid(AssetGuid typed) => typed.Value;
}
*/

