namespace ConcreteEngine.Core.Engine.Render;

[Flags]
public enum EntityDrawMask : byte
{
    None = 0,
    Skinned = 1 << 0,
    Instanced = 1 << 1,
}

public enum EntityCullStatus : byte
{
    Normal = 0,
    AlwaysVisible = 1,
    ForceHidden = 2,
}