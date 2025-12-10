namespace ConcreteEngine.Editor.Store;

public enum EditorItemType : byte
{
    None = 0,
    Unspecified = 1,
    
    // Assets
    Texture,
    Shader,
    Model,
    MaterialTemplate,

    // World
    Entity,
    Component,
    Material,
    
    Particle,
    Animation
}

public readonly record struct EditorId(int Identifier, EditorItemType ItemType)
{
    public static EditorId Empty => new(0, EditorItemType.None);
    
    public bool IsValid => Identifier > 0 && ItemType != EditorItemType.None;
    
    public static implicit operator int(EditorId id) => id.Identifier;
    public static implicit operator EditorId((int, EditorItemType) it) => new (it.Item1, it.Item2);

    public int CompareTo(EditorId other)
    {
        var typeComp = ((byte)ItemType).CompareTo((byte)other.ItemType);
        return typeComp != 0 ? typeComp : Identifier.CompareTo(other.Identifier);
    }
}