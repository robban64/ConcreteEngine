namespace ConcreteEngine.Editor.Store.Resources;

public sealed class EditorEntityResource : EditorResource
{
    public string DisplayName { get; set; } = string.Empty;
    public long Generation { get; set; }
    public EditorId Model { get; set; }
    public EditorId ComponentRef { get; set; }
}
