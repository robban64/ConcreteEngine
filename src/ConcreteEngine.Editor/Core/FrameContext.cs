using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.Core;

internal readonly ref struct FrameContext(Span<byte> buffer, float deltaTime, SceneObjectId selectedSceneId, AssetId selectedAssetId)
{
    public readonly Span<byte> Buffer = buffer;
    public readonly float DeltaTime = deltaTime;
    public readonly SceneObjectId SelectedSceneId = selectedSceneId;
    public readonly AssetId SelectedAssetId = selectedAssetId;
    
    public SpanWriter Writer => new (Buffer);

}