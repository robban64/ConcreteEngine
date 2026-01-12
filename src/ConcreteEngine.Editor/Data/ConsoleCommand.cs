namespace ConcreteEngine.Editor.Data;

internal sealed record ConsoleCommandMeta(string Name, string Description, bool IsNoOp);

internal sealed class ConsoleCommandEntry
{
    public required ConsoleCommandMeta Meta { get; init; }
    public required ConsoleCommandDel Handler { get; init; }
}