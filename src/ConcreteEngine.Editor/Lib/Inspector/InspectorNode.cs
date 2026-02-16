using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.UI.Widgets;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib;

public sealed class InspectorEditorObject(string typeName, Type type)
{
    public string TypeName = typeName;
    public Type Type = type;
    public InspectorHeaderUi Header = new();
    public List<InspectorPropertiesUi> Properties = [];
    public InspectorArrayUi? ArrayUi;
}

public sealed class InspectorHeaderUi
{
    public String8Utf8 Id;
    public String8Utf8 Gen;
    public String16Utf8 Name;
    
    public InspectorPropertiesUi? Popup;
    
    private Popup _popupWidget = new(new Vector2(12f, 10f));

    internal void Draw(in Color4 color, UnsafeSpanWriter sw)
    {
        if (Popup != null)
        {
            if (ImGui.ArrowButton("<"u8, ImGuiDir.Left))
                _popupWidget.State = true;

            ImGui.SameLine();
        }

        ImGui.TextUnformatted(ref sw.Start(" [").Append(Id.GetStringSpan()).Append(":")
            .Append(Gen.GetStringSpan()).Append("]").End());

        ImGui.SameLine();
        ImGui.PushFont(null, 15);
        ImGui.TextColored(color, ref Name.GetRef());
        ImGui.PopFont();
        ImGui.Separator();

        if (Popup is { } popup)
        {
            var pos = new Vector2(ImGui.GetItemRectMin().X - 200, ImGui.GetItemRectMin().Y - 50);
            if (_popupWidget.Begin("##obj-popup"u8, pos))
            {
                popup.Draw();
                _popupWidget.End();
            }
        }
    }
}

public sealed class InspectorPropertiesUi(string typeName, String16Utf8 title)
{
    public string TypeName = typeName;
    public String16Utf8 Title = title;
    public readonly List<UiTextProperty> Properties = [];
    //public bool IsOpen;

    internal void Draw()
    {
        if (!ImGui.CollapsingHeader(ref Title.GetRef())) return;
        foreach (ref var it in CollectionsMarshal.AsSpan(Properties))
        {
            ImGui.TextUnformatted(ref it.Label.GetRef());
            ImGui.SameLine();
            ImGui.TextUnformatted(ref it.Value.GetRef());
        }
    }
}

public sealed class InspectorArrayUi(string fieldName, string typeName, int length)
{
    public readonly string FieldName = fieldName;
    public readonly string TypeName = typeName;

    public String16Utf8 Title = new(fieldName);
    
    public readonly List<UiTextProperty> Fields = new(length);

    internal void Draw(UnsafeSpanWriter sw)
    {
        var id = 0;

        ImGui.TextUnformatted(ref Title.GetRef());
        ImGui.Separator();
        
        foreach (ref var it in CollectionsMarshal.AsSpan(Fields))
        {
            ImGui.PushID(id++);
            if (ImGui.TreeNodeEx(ref it.Label.GetRef(), ImGuiTreeNodeFlags.SpanFullWidth))
            {
                ImGui.TextUnformatted(ref it.Label.GetRef());
                ImGui.SameLine();
                ImGui.TextUnformatted(ref it.Value.GetRef());

                ImGui.TreePop();
            }

            ImGui.PopID();
        }
    }
}

public sealed class InspectorEditorNode
{
}

public sealed class InspectorEditorMap
{
}

public sealed class InspectorObject(string objectName)
{
    public string InspectorName = "";
    public string ObjectName = objectName;

    public int IntId = -1;
    public long Gen = -1;
    public Guid GId = Guid.Empty;

    public readonly List<InspectorBaseItem> Items = new(8);
    public readonly List<InspectorBaseItem> Pending = [];

    public InspectorItem AddItem(string name, string info = "")
    {
        var item = new InspectorItem(name, info);
        Items.Add(item);
        return item;
    }

    public T Add<T>(T item) where T : InspectorBaseItem
    {
        Items.Add(item);
        return item;
    }

    public void EndFrame()
    {
        if (Pending.Count == 0) return;

        Items.AddRange(Pending);
        Pending.Clear();
    }
}

public abstract class InspectorBaseItem(string fieldName, string info)
{
    public readonly string FieldName = fieldName;
    public readonly string Info = info; //

    public abstract Span<UiTextProperty> GetProperties();
    internal abstract void Draw(in FrameContext ctx);
}

public sealed class InspectorItem(string fieldName, string info) : InspectorBaseItem(fieldName, info)
{
    public readonly string FieldName = fieldName;
    public readonly string Info = info; //

    public List<UiTextProperty> Fields = [];
    public override Span<UiTextProperty> GetProperties() => CollectionsMarshal.AsSpan(Fields);

    internal override void Draw(in FrameContext ctx)
    {
        foreach (ref var it in CollectionsMarshal.AsSpan(Fields))
        {
            ImGui.TextUnformatted(ref it.Label.GetRef());
            ImGui.SameLine();
            ImGui.TextUnformatted(ref it.Value.GetRef());
        }
    }
}

public sealed class InspectorArrayItem(
    string fieldName,
    string info,
    Type elementType,
    int length
) : InspectorBaseItem(fieldName, info)
{
    public readonly string FieldName = fieldName;
    public readonly string Info = info; //

    public Type ElementType = elementType;

    public List<UiTextProperty> Fields = new(length);

    public override Span<UiTextProperty> GetProperties() => CollectionsMarshal.AsSpan(Fields);

    internal override void Draw(in FrameContext ctx)
    {
        var sw = ctx.Writer;
        var id = 0;
        foreach (ref var it in CollectionsMarshal.AsSpan(Fields))
        {
            ImGui.PushID(id++);
            if (ImGui.TreeNodeEx(ref sw.Write(FieldName), ImGuiTreeNodeFlags.SpanFullWidth))
            {
                ImGui.TextUnformatted(ref it.Label.GetRef());
                ImGui.SameLine();
                ImGui.TextUnformatted(ref it.Value.GetRef());

                ImGui.TreePop();
            }

            ImGui.PopID();
        }
    }
}

public struct UiTextProperty(String16Utf8 label, String16Utf8 value)
{
    public String16Utf8 Label = label;
    public String16Utf8 Value = value;

    public static UiTextProperty Make(string label, ReadOnlySpan<byte> value, int depth)
        => new(new String16Utf8(label.AsSpan()), new String16Utf8(value));

    public static UiTextProperty Make(ReadOnlySpan<char> label, ReadOnlySpan<char> value, int depth)
        => new(new String16Utf8(label), new String16Utf8(value));
}

public readonly struct FormatOptions(string? format, InspectorTypeKind typeKind = InspectorTypeKind.Unknown)
{
    public readonly string? Format = format;
    public readonly InspectorTypeKind TypeKind = typeKind;
}