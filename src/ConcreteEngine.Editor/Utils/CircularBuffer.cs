namespace ConcreteEngine.Editor.Utils;

public sealed class CircularBuffer<T>
{
    private readonly T[] _buffer;
    private readonly short[] _logicalMapping;
    private readonly int _capacity;

    private readonly Action<int, Span<T>> _callback;

    public int Capacity => _capacity;

    public CircularBuffer(int capacity, Action<int, Span<T>> callback)
    {
        _capacity = capacity;
        _callback = callback;
        _buffer = new T[capacity];
        _logicalMapping = new short[capacity];

        Array.Fill(_logicalMapping, (short)-1);
    }

    public void Invalidate(int logicalStart, int length)
    {
        var startIdx = logicalStart % _capacity;
        var endIdx = startIdx + length;

        if (endIdx <= _capacity)
        {
            _callback(logicalStart, _buffer.AsSpan(startIdx, length));
        }
        else
        {
            var len = _capacity - startIdx;
            _callback(logicalStart, _buffer.AsSpan(startIdx, len));
            _callback(logicalStart + len, _buffer.AsSpan(0, length - len));
        }

        for (var i = 0; i < length; i++)
        {
            _logicalMapping[(logicalStart + i) % _capacity] = (short)(logicalStart + i);
        }
    }

    public ViewEnumerator GetView(int logicalStart, int length)
    {
        int pendingStart = -1, pendingLength = 0;

        for (var i = 0; i < length; i++)
        {
            var index = logicalStart + i;
            var head = index % _capacity;

            if (_logicalMapping[head] != index)
            {
                if (pendingLength == 0) pendingStart = index;
                pendingLength++;
            }
            else if (pendingLength > 0)
            {
                Invalidate(pendingStart, pendingLength);
                pendingLength = 0;
            }
        }

        if (pendingLength > 0)
            Invalidate(pendingStart, pendingLength);

        return new ViewEnumerator(_buffer, logicalStart, length);
    }
    
    public ref struct ViewEnumerator
    {
        private readonly T[] _buffer;
        private readonly int _length;
        private readonly int _capacity;
        private int _currentIndex;
        private int _physicalIndex;

        public ViewEnumerator(T[] buffer, int logicalStart, int length)
        {
            _buffer = buffer;
            _capacity = buffer.Length;
            _length = length;
            _currentIndex = -1;
        
            int startIdx = logicalStart % _capacity;
            _physicalIndex = startIdx == 0 ? _capacity - 1 : startIdx - 1;
        }
    
        public bool MoveNext()
        {
            if (++_currentIndex < _length)
            {
                if (++_physicalIndex == _capacity) _physicalIndex = 0;
                return true;
            }
            return false;
        }

        public readonly ref T Current => ref _buffer[_physicalIndex];
    
        public readonly ViewEnumerator GetEnumerator() => this;

    }
}

