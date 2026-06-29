using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Theme;
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
    public Vector2 WindowPadding = GuiTheme.WindowPadding;

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

        int pushedStyles = 1;
        if (NoBorder)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
            pushedStyles++;
        }
        
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, WindowPadding);
        if (ImGui.Begin(Id, Flags))
        {
            OnDraw();
        }
        ImGui.End();
        
        ImGui.PopStyleVar(pushedStyles);

    }
}
