namespace ConcreteEngine.Core.Engine.Assets.Descriptors;

internal delegate void ReloadAssetDel<in TAsset>(TAsset asset, AssetFile[] files) where TAsset : AssetObject;