using System.Runtime.CompilerServices;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib;

internal abstract class Inspector
{
    public static void DrawCollapseSection(ReadOnlySpan<byte> title, Action draw, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.DefaultOpen)
    {
        if (ImGui.CollapsingHeader(title, flags)) draw();
        ImGui.Spacing();
        ImGui.Separator();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawSection(ReadOnlySpan<byte> title, Action draw)
    {
        ImGui.SeparatorText(title);
        draw();
        ImGui.Spacing();
    }

}

internal abstract class Inspector<TSelf> : Inspector where TSelf : Inspector<TSelf>
{
    public static TSelf Instance { get; private set; } = null!;

    protected Inspector()
    {
        if (Instance != null) throw new InvalidOperationException($"{typeof(TSelf).Name} already initialized.");
        Instance = (TSelf)this;
    }


    /*
    internal static void Initialize(TSelf instance)
    {
        if (Instance is not null)
            throw new InvalidOperationException($"{typeof(TSelf).Name} already initialized.");

        Instance = instance;
    }
    */
}