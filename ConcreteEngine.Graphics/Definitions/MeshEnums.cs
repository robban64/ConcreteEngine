namespace ConcreteEngine.Graphics;


public enum MeshDrawKind : byte
{
    Invalid = 0,
    Arrays = 1,
    Elements = 2,
    Instanced = 3
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

public enum VertexElementFormat : byte
{
    Float1 = 1,
    Float2 = 2,
    Float3 = 3,
    Float4 = 4
}