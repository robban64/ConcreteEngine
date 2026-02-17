using ConcreteEngine.Core.Engine.Editor;

namespace ConcreteEngine.Editor.Lib;

public enum InspectorTypeKind : byte
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

public static class InspectorTypeKindExtensions
{
    extension(InspectorTypeKind kind)
    {
        public bool IsValue()
        {
            return kind switch
            {
                InspectorTypeKind.Primitive or InspectorTypeKind.String or InspectorTypeKind.PrimitiveStruct => true,
                _ => false
            };
        }

        public bool IsObject()
        {
            return kind switch
            {
                InspectorTypeKind.Class or InspectorTypeKind.Struct => true,
                _ => false
            };
        }

        public bool IsCollection()
        {
            return kind switch
            {
                InspectorTypeKind.Array or InspectorTypeKind.Map => true,
                _ => false
            };
        }
    }
}

public sealed class InspectorTypeMeta
{
    public readonly string Name;
    public readonly Type Type;
    public readonly InspectorFieldMeta[] AllFields;
    public readonly InspectorFieldMeta[] ValueFields;
    public readonly InspectorFieldMeta[] ObjectFields;
    public readonly InspectorFieldMeta[] CollectionFields;

    public InspectorTypeMeta(Type type, InspectorFieldMeta[] allFields)
    {
        Type = type;
        Name = type.Name;
        AllFields = allFields;

        int valueLen = 0, objLen = 0, colLen = 0, valueIdx = 0, objIdx = 0, colIdx = 0;
        foreach (var field in AllFields)
        {
            if (field.TypeKind.IsValue()) valueLen++;
            else if (field.TypeKind.IsObject()) objLen++;
            else if (field.TypeKind.IsCollection()) colLen++;
        }

        ValueFields = new InspectorFieldMeta[valueLen];
        ObjectFields = new InspectorFieldMeta[objLen];
        CollectionFields = new InspectorFieldMeta[colLen];

        foreach (var field in AllFields)
        {
            if (field.TypeKind.IsValue()) ValueFields[valueIdx++] = field;
            else if (field.TypeKind.IsObject()) ObjectFields[objIdx++] = field;
            else if (field.TypeKind.IsCollection()) CollectionFields[colIdx++] = field;
        }
    }
}

public sealed class InspectorFieldMeta : IComparable<InspectorFieldMeta>
{
    public readonly InspectorTypeKind TypeKind;

    public InspectorFieldKind FieldKind;

    public readonly Type Type;
    public readonly string Name;
    public readonly string? Format;
    public readonly Func<object, object?> Getter;

    public readonly int MemberIndex;
    public readonly byte TypePriority;
    public readonly bool IsAbstractDerived;

    public InspectorFieldMeta(
        int memberIndex,
        InspectorTypeKind typeKind,
        Type type,
        string name,
        string? format,
        Func<object, object?> getter)
    {
        TypeKind = typeKind;
        Type = type;
        Name = name;
        Format = format;
        Getter = getter;
        MemberIndex = memberIndex;

        IsAbstractDerived = type.DeclaringType is { IsAbstract: true };
        TypePriority = byte.MaxValue;
    }

    public int CompareTo(InspectorFieldMeta? other)
    {
        if (other is null) return 1;

        var c = IsAbstractDerived.CompareTo(other.IsAbstractDerived);
        if (c != 0) return c;
        
        c = TypePriority.CompareTo(other.TypePriority);
        return c != 0 ? c : MemberIndex.CompareTo(other.MemberIndex);
    }
}