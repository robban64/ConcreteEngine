using ConcreteEngine.Editor.Core;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib;

internal abstract class EditorWindow(StateManager state)
{
    private const ImGuiWindowFlags DefaultFlags =
        ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove |
        ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;

    public ImGuiWindowFlags Flags = DefaultFlags;
    public bool Enabled { get; private set; }
    public bool NoBorder;

    protected readonly StateManager State = state;

    public abstract ReadOnlySpan<byte> Id { get; }

    public virtual void OnUpdateDiagnostic() { }
    protected virtual void OnCreate(){}
    protected abstract void OnDraw();

    public void Create()
    {
        OnCreate();
        Enabled = true;
    }

    public void Draw()
    {
        if (!Enabled) return;

        if (NoBorder) ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);

        var open = ImGui.Begin(Id, Flags);
        if (open)
        {
            OnDraw();
        }

        ImGui.End();

        if (NoBorder) ImGui.PopStyleVar();
    }
}