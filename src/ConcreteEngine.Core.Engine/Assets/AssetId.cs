namespace ConcreteEngine.Core.Engine.Assets;

public readonly record struct AssetId(int Value)
{
    public bool IsValid() => Value > 0;
    public int Index() => Value - 1;
    public static implicit operator int(AssetId id) => id.Value;
    public static AssetId Empty = new(0);
}

public readonly record struct AssetFileId(int Value)
{
    public bool IsValid() => Value > 0;
    public int Index() => Value - 1;
    public static implicit operator int(AssetFileId id) => id.Value;
    public static AssetId Empty = new(0);
}

public readonly record struct AssetIndexRef(Guid AssetGId, int Index);