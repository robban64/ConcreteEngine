using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Editor.Lib.Impl;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib;

internal sealed class FieldSegment
{
    public readonly PropertyField[] Fields;
    public string Title;
    public Range32 TitleStrHandle;
    public ushort Width;
    public bool Collapsible;
    public FieldSegment(string title, PropertyField[] fields, int width = 0, bool collapsible = false)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(fields);
        Title = title;
        Fields = fields;
        Width = (ushort)int.Max(0,width);
        Collapsible = collapsible;
    }
}

internal abstract unsafe class InspectorFields<T>
{
    private readonly int _id = Guid.NewGuid().GetHashCode();
    private int _segmentIdx;

    private readonly FieldSegment[] _segments = [];
    private readonly List<PropertyField> _fields = new(8);

    private MemoryBlockPtr _memory;

    protected virtual FieldLayout DefaultLayout { get; } = FieldLayout.None;
    protected virtual FieldGetDelay DefaultDelay { get; } = FieldGetDelay.None;


    protected InspectorFields(int segmentCount)
    {
        if (segmentCount > 0)
        {
            _segments = new FieldSegment[segmentCount];
        }

    }

    public void Allocate(ArenaAllocator allocator)
    {
        var builder = allocator.AllocBuilder();
        foreach(var it in _segments)
        {
            it.TitleStrHandle = builder.AllocStringSlice(it.Title).AsRange32();
        }
        _memory = builder.Commit();

        foreach (var it in _fields)
        {
            it.Allocate(allocator);
        }
    }

    public abstract void Bind(T target);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Unbind()
    {
        foreach (var it in _fields)
            it.GetBinding().Unbind();
    }

    public void Refresh()
    {
        foreach (var it in _fields)
            it.Refresh();
    }

    public bool Draw(int start = 0, int end = 0)
    {
        var changed = false;
        var len = end > 0 ? end : _segments.Length;

        if ((uint)start >= (uint)_segments.Length || (uint)end >= (uint)_segmentIdx)
            throw new ArgumentOutOfRangeException(nameof(end));

        ImGui.PushID(_id);
        for (var i = start; i < len; i++)
        {
            var segment = _segments[i];
            var width = segment.Width;
            var title = _memory.DataPtr + segment.TitleStrHandle.Offset;
            bool visible = true;

            ImGui.Spacing();

            if(segment.Collapsible)
                visible = ImGui.CollapsingHeader(title);
            else 
                ImGui.SeparatorText(title);

            if(!visible) continue;


            if (width > 0) ImGui.PushItemWidth(width);
            foreach (var it in segment.Fields)
            {
                changed |= it.Draw();
            }
            if (width > 0) ImGui.PopItemWidth();
        }
        ImGui.PopID();
        return changed;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    protected TField Register<TField>(TField field, FieldGetDelay? delay = null, FieldLayout? layout = null)
        where TField : PropertyField
    {
        ArgumentNullException.ThrowIfNull(field);

        if (delay.HasValue) field.Delay = delay.Value;
        else if (DefaultDelay != FieldGetDelay.None) field.Delay = DefaultDelay;

        if (layout.HasValue) field.Layout = layout.Value;
        else if (DefaultLayout != FieldLayout.None) field.Layout = DefaultLayout;

        _fields.Add(field);
        return field;
    }

    protected void CreateSegment(string title, PropertyField[] fields) => CreateSegment(title, false, 0, fields);


    [MethodImpl(MethodImplOptions.NoInlining)]
    protected void CreateSegment(string title, bool collapsible, int width, PropertyField[] fields)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentOutOfRangeException.ThrowIfZero(fields.Length, nameof(fields));

        _segments[_segmentIdx++] = new FieldSegment(title, fields, width, collapsible);
    }
}

