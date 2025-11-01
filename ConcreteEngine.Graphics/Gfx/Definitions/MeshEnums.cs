namespace ConcreteEngine.Graphics.Gfx.Definitions;

public enum DrawMeshKind : byte
{
    Invalid = 0,
    Arrays = 1,
    Elements = 2,
    ArraysInstanced = 3
}

public enum DrawElementSize : byte
{
    Invalid = 0,
    UnsignedByte = 1,
    UnsignedShort = 2,
    UnsignedInt = 4
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
    Float = 0,
    UByte = 1,
    UShort = 2
}