using System.Numerics;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib.Inputs;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib;

internal abstract class Inspector
{
    protected InspectSection[] Sections { get; init; } = [];

    public abstract InspectorId Id { get; }
    public abstract uint Icon { get; }
    public abstract void DetachTarget();

    public virtual void Draw()
    {
        foreach (var section in Sections) section.Draw();
    }
}

internal abstract class Inspector<TTarget> : Inspector  where TTarget : class
{
    public static Inspector<TTarget> Instance { get; private set; } = null!;
    public static TTarget? Target { get; private set; }
    
    protected virtual void OnAttachTarget(TTarget? oldTarget, TTarget newTarget){}
    protected virtual void OnDetachTarget(TTarget target){}


    protected Inspector()
    {
        if (Instance != null) throw new InvalidOperationException($"{typeof(Inspector<TTarget>).Name} already initialized.");
        Instance = this;
        
        //var name = StringArena.AllocateString(typeof(TSelf).Name);
        //Header = new InspectorHeader(name,IconNames.Circle, Palette.CyanBase);
    }
    
    public void AttachTarget(TTarget target)
    {
        var oldTarget = Target;
        if (oldTarget == target) return;
        Target = target;
        OnAttachTarget(oldTarget, target);
    }

    public override void DetachTarget()
    {
        var oldTarget = Target;
        if (oldTarget == null) return;
        Target = null;
        OnDetachTarget(oldTarget);
    }
    
/*
    public void DrawInspector(InspectSection[] sections)
    {
        Header?.Draw();
        ImGui.Spacing();
        
        var contentWidth = ImGui.GetContentRegionAvail().X;
        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0, 0.5f));
        foreach (var section in sections)
        {
            ImGui.Spacing();
            section.Draw(contentWidth);
        }
        ImGui.PopStyleVar();
    }*/
}