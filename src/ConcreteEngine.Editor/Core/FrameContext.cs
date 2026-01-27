using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.Core;

internal readonly ref struct FrameContext(
    in StrWriter8 writer,
    float deltaTime,
    SceneObjectId selectedSceneId,
    AssetId selectedAssetId)
{
    private readonly StrWriter8 _writer = writer;
    public readonly float DeltaTime = deltaTime;
    public readonly SceneObjectId SelectedSceneId = selectedSceneId;
    public readonly AssetId SelectedAssetId = selectedAssetId;

    public StrWriter8 Writer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _writer;
    }
}