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

public sealed class InspectorTypeMeta
{
    public string Name;
    public Type Type;
    public bool IsStruct;
    public InspectorFieldMeta[] FieldMetadata;
}

public sealed class InspectorFieldMeta : IComparable<InspectorFieldMeta>
{
    public readonly InspectorTypeKind TypeKind;
    public readonly Type Type;
    public readonly string Name;
    public readonly string? Format;
    public readonly Func<object, object?> Getter;

    public readonly int MemberIndex;
    public readonly byte TypePriority;
    public readonly bool IsAbstractDerived;

    public InspectorFieldMeta(int memberIndex,
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
        if (typeKind is InspectorTypeKind.Struct)
            TypePriority = 2;
        else if (typeKind is InspectorTypeKind.Class or InspectorTypeKind.Array or InspectorTypeKind.Map)
            TypePriority = 1;
    }

    public int CompareTo(InspectorFieldMeta? other)
    {
        if (other is null) return 1;

        var c = TypePriority.CompareTo(other.TypePriority);
        if (c != 0) return c;

        c = IsAbstractDerived.CompareTo(other.IsAbstractDerived);
        return c != 0 ? c : MemberIndex.CompareTo(other.MemberIndex);
    }
}