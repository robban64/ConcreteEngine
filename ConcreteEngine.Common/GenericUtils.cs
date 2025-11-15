namespace ConcreteEngine.Common;

public static class GenericUtils
{
    public static bool FailOut<T>(out T? value) where T : class
    {
        value = null;
        return false;
    }

    public static bool FailOutValue<T>(out T value) where T : unmanaged
    {
        value = default;
        return false;
    }
}