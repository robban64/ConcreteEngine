using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Components;
using ConcreteEngine.Editor.Components.State;
using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor.Data;

internal abstract class ComponentEvent(EventKey eventKey)
{
    public EventKey EventKey { get; } = eventKey;
    public abstract Type ComponentType { get; }
}

internal sealed class SceneObjectEvent(EventKey eventKey, SceneObjectId sceneObject) : ComponentEvent(eventKey)
{
    public SceneObjectId SceneObject { get; } = sceneObject;
    public override Type ComponentType => typeof(SceneComponent);
}

internal sealed class AssetEvent(EventKey eventKey, AssetId asset) : ComponentEvent(eventKey)
{
    public AssetId Asset { get; } = asset;
    public string? Name { get; init; }
    public override Type ComponentType => typeof(AssetsComponent);
}

internal sealed class WorldEvent(EventKey eventKey, WorldState state) : ComponentEvent(eventKey)
{
    //public static readonly WorldEvent CommitDataInstance  = new (EventKey.CommitData);
    public WorldSelection? Selection { get; init; }
    public WorldState State { get; } = state;
    public override Type ComponentType => typeof(VisualComponent);
}

internal sealed class VisualDataEvent(SlotState<EditorVisualState> state) : ComponentEvent(EventKey.CommitData)
{
    public readonly SlotState<EditorVisualState> State = state;
    public override Type ComponentType => typeof(VisualComponent);
}

internal sealed class GraphicsSettingsEvent() : ComponentEvent(EventKey.GraphicsSetting)
{
    public int? ShadowSize { get; init; }
    public override Type ComponentType => typeof(VisualComponent);
}