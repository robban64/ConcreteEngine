using ConcreteEngine.Engine.Configuration;

namespace ConcreteEngine.Engine.Utils;

internal struct EngineSetupStepper
{
    public int WarmupTick;
    private int _currentIndex;
    private readonly int _endIndex;

    public EngineSetupStepper(int endIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(endIndex);
        _currentIndex = 0;
        _endIndex = endIndex;
    }

    public EngineStateLevel Current => (EngineStateLevel)_currentIndex;

    public EngineStateLevel Next(bool condition = true)
    {
        if (condition && _currentIndex < _endIndex - 1)
            _currentIndex++;

        return Current;
    }
}