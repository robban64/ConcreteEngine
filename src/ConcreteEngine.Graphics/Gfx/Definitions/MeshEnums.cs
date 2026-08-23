namespace ConcreteEngine.Graphics.Gfx;

public enum DrawMeshKind : byte
{
    Arrays = 0,
    Elements = 1,
    ArraysInstanced = 2,
    ElementsInstanced = 3,
}

public enum DrawElementSize : byte
{
    None = 0,
    UnsignedByte = 1,
    UnsignedShort = 2,
    UnsignedInt = 3
}

public enum DrawPrimitive : byte
{
    Triangles = 0,
    TriangleStrip = 1,
    TriangleFan = 2,
    Points = 3,
    Lines = 4,
    LineLoop = 5,
    LineStrip = 6
}

public enum VertexFormat : byte
{
    Invalid,
    Float,
    Int,
    UInt,
    UByte,
    UShort,
    Half,
}