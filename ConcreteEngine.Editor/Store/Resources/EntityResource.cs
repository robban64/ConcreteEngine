namespace ConcreteEngine.Editor.Store.Resources;

public sealed class EditorEntityResource : EditorResource
{
    public string DisplayName { get; set; } = string.Empty;
    public EditorId Model { get; set; }
    public EditorId Material { get; set; }
    public EditorId[] Materials { get; set; } = [];
    public EditorEntityComponentKind[] Components = [];
    public long Generation { get; set; }
}

public enum EditorEntityComponentKind
{
    Core,
    Animation,
    Particle
}