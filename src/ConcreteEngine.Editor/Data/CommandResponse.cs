namespace ConcreteEngine.Editor.Data;

public readonly ref struct CommandResponse(bool success, string? error)
{
    public readonly bool Success = success;
    public readonly string? Error = error;
    public static CommandResponse Ok() => new(true, null);
    public static CommandResponse Fail(string error) => new(false, error);
}