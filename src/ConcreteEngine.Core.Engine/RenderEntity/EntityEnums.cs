namespace ConcreteEngine.Core.Engine.RenderEntity;

[Flags]
public enum EntityDrawFlags : byte
{
    None = 0,
    Skinned = 1 << 0,
    Instanced = 1 << 1,
}


public enum EntityStatus : byte
{
    Unset = 0,
    ForceHidden = 1,
    Normal = 2,
    AlwaysVisible = 3,
}