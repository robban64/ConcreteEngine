namespace ConcreteEngine.Core.Engine.Assets.Descriptors;

internal delegate void ReloadAssetDel<in TAsset>(TAsset asset, AssetFileSpec[] files) where TAsset : AssetObject;