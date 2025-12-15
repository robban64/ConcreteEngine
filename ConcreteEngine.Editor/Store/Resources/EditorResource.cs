namespace ConcreteEngine.Editor.Store.Resources;

public abstract class EditorResource : IComparable<EditorResource>
{
    public required EditorId Id { get; init; }
    public required string Name { get; init; }

    public int CompareTo(EditorResource? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        return other is null ? 1 : Id.CompareTo(other.Id);
    }
}