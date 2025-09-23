#region

using System.Runtime.CompilerServices;

#endregion

namespace ConcreteEngine.Graphics.Primitives;

public struct BufferDataBlueprint<TElement> where TElement : unmanaged
{
    private readonly int _elementStride;
    private readonly int _elementCount;

    private int _componentStride = 0;

    public BufferDataBlueprint(int elementCount)
    {
        _elementStride = Unsafe.SizeOf<TElement>();
        _elementCount = elementCount;
    }

    // result will be returned here, source of truth for the metadata that populate those returned values
    public void GetStrideAndSize()
    {
        nint stride = _elementStride;
        nint size = (nint)_elementStride * _elementCount;
    }

    public void AsPrimitive<TPrimitive>() where TPrimitive : unmanaged
    {
        if (_componentStride != 0) throw new InvalidOperationException("Primitive already set.");

        int primitiveSize = Unsafe.SizeOf<TElement>();
        if (primitiveSize != 1 && primitiveSize != 2 && primitiveSize != 4)
            throw new ArgumentOutOfRangeException(nameof(primitiveSize), "Invalid primitive size.");
        if (_elementStride % primitiveSize != 0)
            throw new InvalidOperationException("Element size must be a multiple of primitive size.");

        _componentStride = primitiveSize;
    }
}