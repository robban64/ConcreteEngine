using System.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib.Inputs;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib;

internal abstract class Inspector
{
    protected InspectSection[] Sections { get; init; } = [];

    public abstract InspectorId Id { get; }
    
    public virtual uint Icon { get; }
    
    public abstract void DetachTarget();

    public virtual void Draw() => DrawSections();

    protected void DrawSections()
    {
        foreach (var section in Sections) section.Fetch(EditorTime.Delta);
        
        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0, 0.5f));
        var contentWidth = ImGui.GetContentRegionAvail().X;
        foreach (var section in Sections)
        {
            ImGui.PushID(section.Id);
            section.Draw(contentWidth);
            ImGui.PopID();
        }

        ImGui.PopStyleVar();
    }

    protected void ApplySectionLowFetchRate()
    {
        foreach (var section in Sections) section.SetFetchRateLow();
    }
    protected void ApplySectionHighFetchRate()
    {
        foreach (var section in Sections) section.SetFetchRateHigh();
    }

}

internal abstract class Inspector<TTarget> : Inspector where TTarget : class
{
    public static Inspector<TTarget> Instance { get; private set; } = null!;
    public static TTarget? Target { get; private set; }

    protected virtual void OnAttachTarget(TTarget? oldTarget, TTarget newTarget) { }
    protected virtual void OnDetachTarget(TTarget target) { }

    protected Inspector()
    {
        if (Instance != null)
            throw new InvalidOperationException($"{typeof(Inspector<TTarget>).Name} already initialized.");
        
        Instance = this;
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
}