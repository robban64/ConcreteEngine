using System.Reflection;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Editor;

namespace ConcreteEngine.Editor.Lib;

public enum InspectorTypeKind
{
    Unknown,
    Primitive,
    PrimitiveStruct,
    String,
    Array,
    Map,
    Struct,
    Class,
}

public sealed class InspectorFieldMeta(
    InspectorTypeKind typeKind,
    Type type,
    string name,
    string? format,
    Func<object, object?> getter)
{
    public readonly InspectorTypeKind TypeKind = typeKind;
    public readonly Type Type = type;
    public readonly string Name = name;
    public readonly string? Format = format;

    public readonly Func<object, object?> Getter = getter;
}

public sealed class InspectorObject
{
    public string InspectorName;
    public string ObjectName;

    public int IntId = -1;
    public long Gen = -1;
    public Guid GId = Guid.Empty;
}

public abstract class InspectorComponent(string typeName, string fieldName)
{
    public string TypeName = typeName;
    public string FieldName = fieldName;
}

public sealed class InspectorFields(string typeName, string fieldName, Row[] fieldRows)
    : InspectorComponent(typeName, fieldName)
{
    public Row[] FieldRows = fieldRows;
}

public sealed class InspectorArray(string typeName, string fieldName, List<Row> entries)
    : InspectorComponent(typeName, fieldName)
{
    public List<Row> Entries = entries;
}

public sealed class InspectorMap(string typeName, string fieldName, List<Row> entries)
    : InspectorComponent(typeName, fieldName)
{
    public List<Row> Entries = entries;
}

public struct Row(String16Utf8 label, String16Utf8 value, int depth)
{
    public String16Utf8 Label = label;
    public String16Utf8 Value = value;
    public int Depth = depth;

    public static Row Make(ReadOnlySpan<byte> label, ReadOnlySpan<byte> value, int depth)
        => new(new String16Utf8(label), new String16Utf8(value), depth);

    public static Row Make(string label, ReadOnlySpan<byte> value, int depth)
        => new(new String16Utf8(label.AsSpan()), new String16Utf8(value), depth);
}

public readonly struct FormatOptions(string? format, InspectorTypeKind typeKind = InspectorTypeKind.Unknown)
{
    public readonly string? Format = format;
    public readonly InspectorTypeKind TypeKind = typeKind;
}