namespace ConcreteEngine.Common;

public class LinearStateMachine<T> where T : Enum
{

    private readonly T[] _states;
    private int _currentIndex;

    public LinearStateMachine(params T[] states)
    {
        if (states == null || states.Length == 0)
            throw new ArgumentException("At least one state must be provided.", nameof(states));
        
        _states = states;
        _currentIndex = 0;
    }

    public T Current => _states[_currentIndex];

    public T Next(bool condition = true)
    {
        
        if (condition && _currentIndex < _states.Length - 1)
            _currentIndex++;

        return Current;
    }
}
