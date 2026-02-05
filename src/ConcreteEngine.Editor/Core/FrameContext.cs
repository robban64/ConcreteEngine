using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;

namespace ConcreteEngine.Editor.Core;

internal readonly struct FrameContext(
    in NativeArray<byte> buffer,
    float deltaTime,
    SceneObjectId selectedSceneId,
    AssetId selectedAssetId)
{
    private readonly NativeArray<byte> _buffer = buffer;
    public readonly float DeltaTime = deltaTime;
    public readonly SceneObjectId SelectedSceneId = selectedSceneId;
    public readonly AssetId SelectedAssetId = selectedAssetId;

    public UnsafeSpanWriter Writer => new(in _buffer);
}