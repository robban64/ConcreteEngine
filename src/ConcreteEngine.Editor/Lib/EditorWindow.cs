using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib;

internal abstract class EditorWindow(StateManager state)
{
    private const ImGuiWindowFlags DefaultFlags =
        ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove |
        ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;

    public ImGuiWindowFlags Flags = DefaultFlags;
    public bool NoBorder;

    public bool Enabled = true;
    
    protected readonly StateManager State = state;

    public abstract ReadOnlySpan<byte> Id { get; }

    protected abstract void OnDraw();
    public abstract void OnCreate();
    
    public virtual void OnUpdateDiagnostic(){}

    public void Draw()
    {
        if(!Enabled) return;
        
        if (NoBorder) ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);

        if (ImGui.Begin(Id, Flags))
        {
            OnDraw();
        }
        ImGui.End();

        if (NoBorder) ImGui.PopStyleVar();
    }
}
