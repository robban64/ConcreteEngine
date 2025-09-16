namespace ConcreteEngine.Graphics.Resources;

// DONT CHANGE THIS!!!!
public enum UniformGpuSlot
{
    Frame     = 0,
    Camera    = 1,
    DirLight  = 2,
    Material  = 3,
    DrawObject= 4,
}

public enum UboDefaultCapacity
{
    Lower,
    Medium,
    Upper
}