namespace ConcreteEngine.Core.Assets;

public readonly record struct AssetId(int Value);
public readonly record struct AssetFileId(int Value);

public readonly record struct AssetBindingKey(AssetId Asset, AssetFileId AssetFile);