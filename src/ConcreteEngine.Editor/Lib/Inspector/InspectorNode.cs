using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Theme.Widgets;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib.Inspector;

public sealed class InspectorEditorObjectV2(string typeName, Type type)
{
    public Type Type = type;

    public string TypeName = typeName;
    public InspectorHeaderUi Header = new();
    internal List<InspectorSection> Sections = [];
}

internal abstract class InspectorSection(string sectionName, String16Utf8 title)
{
    public string SectionName = sectionName;
    public String16Utf8 Title = title;
    public bool FullSection;

    public void Draw(FrameContext ctx)
    {
        if (FullSection)
        {
            ImGui.SeparatorText(ref Title.GetRef());
        }
        else
        {
            ImGui.TextUnformatted(ref Title.GetRef());
            ImGui.Separator();
        }

        OnDraw(ctx);
    }

    protected abstract void OnDraw(FrameContext ctx);
}

internal sealed class InspectorTreeSection(string sectionName, String16Utf8 title)
    : InspectorSection(sectionName, title)
{
    public readonly List<String16Utf8> Rows = [];
    public readonly List<InspectorSection> RowSections = [];

    protected override void OnDraw(FrameContext ctx)
    {
        var rows = CollectionsMarshal.AsSpan(Rows);
        var len = rows.Length;
        for (int i = 0; i < len; i++)
        {
            ImGui.PushID(i);
            if (ImGui.TreeNodeEx(ref rows[i].GetRef(), ImGuiTreeNodeFlags.SpanFullWidth))
            {
                RowSections[i].Draw(ctx);
                ImGui.TreePop();
            }

            ImGui.PopID();
        }
    }
}

internal sealed class InspectorTableSection(string sectionName, String16Utf8 title)
    : InspectorSection(sectionName, title)
{
    public readonly String16Utf8[] Columns = [];
    public readonly String16Utf8[][] Rows = [];

    protected override void OnDraw(FrameContext ctx)
    {
        var id = 0;

        if (!ImGui.BeginTable("##cubemap_faces"u8, 2, GuiTheme.TableFlags)) return;

        foreach (ref var it in Columns.AsSpan())
        {
            ImGui.TableSetupColumn(ref it.GetRef());
        }

        ImGui.TableHeadersRow();

        for (int i = 0; i < Rows.Length; i++)
        {
            var row = Rows[i];
            for (int j = 0; j < row.Length; j++)
            {
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(ref row[j].GetRef());
            }
        }
    }
}

public sealed class InspectorEditorObject(string typeName, Type type)
{
    public Type Type = type;

    public string TypeName = typeName;
    public InspectorHeaderUi Header = new();
    public List<InspectorSectionUi> Sections = [];
    public InspectorArrayUi? ArrayUi;

    internal List<InspectorSection> Sectionss = [];
}

public sealed class InspectorHeaderUi
{
    public String8Utf8 Id;
    public String8Utf8 Gen;
    public String16Utf8 Name;

    public InspectorSectionUi? Popup;

    private Popup _popupWidget = new(new Vector2(12f, 10f));

    internal void Draw(in Color4 color, UnsafeSpanWriter sw)
    {
        if (Popup != null)
        {
            if (ImGui.ArrowButton("<"u8, ImGuiDir.Left))
                _popupWidget.State = true;

            ImGui.SameLine();
        }

        ImGui.TextUnformatted(ref sw.Start(" [").Append(Id.AsSpan()).Append("[")
            .Append(Gen.AsSpan()).Append("]").End());

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

public sealed class InspectorSectionUi(string typeName)
{
    public readonly string TypeName = typeName;
    public String32Utf8 Title;
    public readonly List<UiTextProperty> Properties = [];

    internal void Draw()
    {
        if (!ImGui.CollapsingHeader(ref Title.GetRef())) return;
        foreach (ref var it in CollectionsMarshal.AsSpan(Properties))
        {
            ImGui.TextUnformatted(ref it.Label.GetRef());
            ImGui.SameLine();
            ImGui.TextUnformatted(ref it.Value.GetRef());
        }

        ImGui.Separator();
    }
}

public sealed class InspectorArrayUi(string fieldName, string typeName, int length)
{
    public readonly string FieldName = fieldName;
    public readonly string TypeName = typeName;

    public String16Utf8 Title = new(fieldName);

    public readonly List<InspectorSectionUi> Fields = new(length);

    internal void Draw(UnsafeSpanWriter sw)
    {
        var id = 0;

        ImGui.SeparatorText(ref Title.GetRef());

        foreach (var it in Fields)
        {
            ImGui.PushID(id++);
            if (ImGui.TreeNodeEx(ref it.Title.GetRef(), ImGuiTreeNodeFlags.SpanFullWidth))
            {
                foreach (ref var prop in CollectionsMarshal.AsSpan(it.Properties))
                {
                    ImGui.TextUnformatted(ref prop.Label.GetRef());
                    ImGui.SameLine();
                    ImGui.TextUnformatted(ref prop.Value.GetRef());
                }

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