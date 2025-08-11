namespace ConcreteEngine.Common.Collections;

public class ArrayListBuffer<T> where T : class
{
    private readonly T[] _buffer;
    private readonly int _bufferSize;

    public ArrayListBuffer(int bufferSize)
    {
        _buffer = new T[bufferSize];
        _bufferSize = bufferSize;
    }
}