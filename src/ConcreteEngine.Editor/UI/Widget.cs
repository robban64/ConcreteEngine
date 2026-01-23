using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.UI;

internal abstract class Widget
{
    private static int _idCounter = 100_000;
    protected readonly int Id = _idCounter++;
}