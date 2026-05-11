using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Engine.Assets.Data;

internal delegate void ReloadAssetDel<in TAsset>(TAsset asset, AssetFileSpec[] files) where TAsset : AssetObject;