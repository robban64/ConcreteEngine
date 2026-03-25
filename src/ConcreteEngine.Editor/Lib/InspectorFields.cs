using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib.Impl;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Lib;

internal sealed class InspectorFieldProvider
{
    public static InspectorFieldProvider Instance = null!;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Create()
    {
        if (Instance != null) throw new InvalidOperationException("Instance is not null");
        Instance = new InspectorFieldProvider();
    }

    private InspectorFieldProvider() { }

    public readonly InspectSceneFields SceneFields = new();
    public readonly InspectModelInstanceFields ModelInstanceFields = new();
    public readonly InspectParticleFields ParticleInstanceFields = new();

    public readonly InspectMaterialFields MaterialFields = new();
    public readonly InspectTextureFields TextureFields = new();

    public readonly InspectCameraFields CameraFields = new();
    public readonly InspectLightningFields LightningFields = new();
    public readonly InspectPostFxFields PostFxFields = new();

}
internal sealed class FieldSegment
{
    public const int AllocSize = 16;
    public readonly NativeViewPtr<byte> Title;
    public readonly PropertyField[] Fields;
    public int Width;
    public bool Collapsible;

    public FieldSegment(NativeViewPtr<byte> title, PropertyField[] fields, int width = 0, bool collapsible = false)
    {
        if(title.IsNull) throw new ArgumentNullException(nameof(title));
        ArgumentNullException.ThrowIfNull(fields);
        Title = title;
        Fields = fields;
        Width = width;
        Collapsible = collapsible;
    }
}


internal abstract unsafe class InspectorFields<T>
{
    private readonly int _id = Guid.NewGuid().GetHashCode();
    public int Width = 0;
    private readonly FieldSegment[] _segments = [];
    private readonly List<PropertyField> _fields = new(8);

    private ArenaBlock* _allocator;
    private int _segmentIdx;

    protected InspectorFields(int segmentCount)
    {
        if (segmentCount > 0)
        {
            _segments = new FieldSegment[segmentCount];
            _allocator = TextBuffers.PersistentArena.Alloc(segmentCount * FieldSegment.AllocSize);
        }
        else
        {
            _allocator = null;
        }

    }

    protected virtual FieldLayout DefaultLayout { get; } = FieldLayout.None;
    protected virtual FieldGetDelay DefaultDelay { get; } = FieldGetDelay.None;
    public abstract void Bind(T target);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Unbind()
    {
        foreach (var it in _fields)
            it.Unbind();
    }

    public void Refresh()
    {
        foreach (var it in _fields)
            it.Refresh();
    }

    public bool Draw(RangeU16 segmentRange = default)
    {
        int end = int.Clamp(segmentRange.End, 1, _segments.Length);
        bool changed = false;
        ImGui.PushID(_id);
        for (int i = segmentRange.Offset; i < end; i++)
        {
            var segment = _segments[i];
            var width = segment.Width;

            ImGui.Spacing();
            if (segment.Collapsible)
            {
                if (!ImGui.CollapsingHeader(segment.Title))
                    continue;
            }
            else
            {
                ImGui.SeparatorText(segment.Title);
            }

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

        var titlePtr = _allocator->AllocSlice(16);
        titlePtr.Writer().Write(title);
        _segments[_segmentIdx++] = new FieldSegment(titlePtr, fields, width, collapsible);
    }

}

