namespace ConcreteEngine.Graphics.Resources;

// DONT CHANGE THIS!!!!
public enum UniformGpuSlot : byte
{
    Frame     = 0,
    Camera    = 1,
    DirLight  = 2,
    Material  = 3,
    DrawObject= 4,
}

public enum UboDefaultCapacity
{
    Lower = 0,
    Medium = 1,
    Upper = 2
}