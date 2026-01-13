using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.Assets.Descriptors;

namespace ConcreteEngine.Engine.Assets.Data;


internal delegate void ReloadAssetDel<in TAsset>(TAsset asset, AssetFileSpec[] prevSpecs, out AssetFileSpec[] specs)
    where TAsset : AssetObject;