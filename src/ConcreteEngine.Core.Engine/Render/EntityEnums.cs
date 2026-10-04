namespace ConcreteEngine.Core.Engine.Render;

[Flags]
public enum EntityDrawFlags : byte
{
    None = 0,
    Skinned = 1 << 0,
    Instanced = 1 << 1,
}

public enum DrawStatus : byte
{
    Normal = 0,
    AlwaysVisible = 1,
    ForceHidden = 2,
}