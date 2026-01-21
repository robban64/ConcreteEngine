using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.Core;

internal ref struct FrameContext(SpanWriter sw, float deltaTime, SceneObjectId selectedSceneId, AssetId selectedAssetId)
{
    public SpanWriter Sw = sw;
    public readonly float DeltaTime = deltaTime;
    public readonly SceneObjectId SelectedSceneId = selectedSceneId;
    public readonly AssetId SelectedAssetId = selectedAssetId;
}