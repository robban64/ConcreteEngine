namespace ConcreteEngine.Editor.Components.Data;

public enum EditorItemType : byte
{
    None = 0,
    
    // Assets
    Texture,
    Shader,
    Mesh,
    MaterialTemplate,

    // World
    Entity,
    Component,
    Material,
}
public readonly record struct EditorId(int Identifier, ushort Gen, EditorItemType ItemType);