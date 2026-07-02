using System.Numerics;
using ConcreteEngine.Editor.App.Theme;
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
    
    public virtual void OnUpdateDiagnostic(){}
    protected abstract void OnCreate();
    protected abstract void OnDraw();

    public void Create()
    {
        OnCreate();
        Enabled = true;
    }

    public void Draw()
    {
        if(!Enabled) return;

        if (NoBorder) ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
        
        if (ImGui.Begin(Id, Flags))
        {
            OnDraw();
        }
        ImGui.End();
        
        if(NoBorder) ImGui.PopStyleVar();

    }
}
