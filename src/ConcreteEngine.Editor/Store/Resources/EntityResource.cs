namespace ConcreteEngine.Editor.Store.Resources;

public sealed class EditorEntityResource : EditorResource
{
    public string DisplayName { get; set; } = string.Empty;
    public EditorId Model { get; set; }
    public EditorId ComponentRef { get; set; }
}

public sealed class EditorSceneObject : EditorResource
{
    public required Guid EngineGid { get; init; }
    public required bool Enabled;

    public required int GameEcsCount;
    public required int RenderEcsCount;
}