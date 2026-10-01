namespace ConcreteEngine.Core.Engine.ECS.Render;

[Flags]
public enum EntityDrawFlags : byte
{
    None = 0,
    Skinned = 1 << 0,
    Instanced = 1 << 1,
}

public enum EntityDrawStatus : byte
{
    Normal = 0,
    AlwaysVisible = 1,
    ForceHidden = 2,
}