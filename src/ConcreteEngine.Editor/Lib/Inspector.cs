using System.Numerics;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib.Inputs;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib;

internal abstract class Inspector
{
    public abstract void Draw();
}

internal abstract class Inspector<TSelf> : Inspector  where TSelf : Inspector<TSelf>
{
    public static TSelf Instance { get; private set; } = null!;

    //protected InspectorHeader Header;
    //protected InspectSection[] Sections;
    
    
    public abstract uint Icon { get; }
    
    protected Inspector()
    {
        if (Instance != null) throw new InvalidOperationException($"{typeof(TSelf).Name} already initialized.");
        Instance = (TSelf)this;
        
        //var name = StringArena.AllocateString(typeof(TSelf).Name);
        //Header = new InspectorHeader(name,IconNames.Circle, Palette.CyanBase);
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