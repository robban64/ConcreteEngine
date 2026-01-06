namespace ConcreteEngine.Core.Engine.Assets;

public readonly record struct AssetId(int Value)
{
    public bool IsValid() => Value > 0 ;
    public static implicit operator int(AssetId id) => id.Value;
}

public readonly record struct AssetFileId(int Value)
{
    public bool IsValid() => Value > 0;
}
