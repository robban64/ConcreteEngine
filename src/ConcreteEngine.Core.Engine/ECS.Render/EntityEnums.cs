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
    Unset = 0,
    ForceHidden = 1,
    Normal = 2,
    AlwaysVisible = 3,
}