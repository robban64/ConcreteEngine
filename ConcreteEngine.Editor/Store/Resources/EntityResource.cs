namespace ConcreteEngine.Editor.Store.Resources;

public sealed class EditorEntityResource : EditorResource
{
    public string DisplayName { get; set; } = string.Empty;
    public int Model { get; set; }
    public int Material { get; set; }
    public int[] Materials { get; set; } = [];
    public int ComponentCount { get; }
    public long Generation { get; set; }
}