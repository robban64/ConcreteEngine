using Microsoft.CodeAnalysis;

namespace Generator;

internal readonly record struct MemberInfo(bool IsField, bool IsProperty, bool IsReadOnly, bool ReturnRef, MemberTypeInfo TypeInfo)
{
    public bool ReturnRef { get; init; }
    
    public bool IsClassProperty() => IsProperty && TypeInfo.IsClass();
    public bool IsStructProperty() => IsProperty && !ReturnRef && TypeInfo.IsStruct();
    public bool IsRefProperty() => IsProperty && ReturnRef;

    public static MemberInfo Extract(ISymbol sym)
    {
        if (sym is IPropertySymbol prop)
        {
            var retRef = prop.ReturnsByRef || prop.ReturnsByRefReadonly;
            return new MemberInfo(false, true, prop.IsReadOnly, retRef, MemberTypeInfo.Extract(prop.Type));
        }

        if (sym is IFieldSymbol field)
        {
            return new MemberInfo(true, false, field.IsReadOnly, false, MemberTypeInfo.Extract(field.Type));
        }

        throw new ArgumentException(nameof(sym));
    }
}

internal readonly record struct MemberTypeInfo(
    bool IsValueType,
    bool IsUnmanaged,
    bool IsReadOnly,
    bool IsRef,
    TypeKind TypeKind,
    SpecialType SpecialType,
    SymbolKind Symbol)
{
    public bool IsStruct() => TypeKind == TypeKind.Struct;
    public bool IsClass() => TypeKind == TypeKind.Class;

    public bool IsBlitStruct => IsUnmanaged && TypeKind == TypeKind.Struct;

    public static MemberTypeInfo Extract(ITypeSymbol type)
    {
        return new MemberTypeInfo(
            type.IsValueType,
            type.IsUnmanagedType,
            type.IsReadOnly,
            type.IsRefLikeType,
            type.TypeKind,
            type.SpecialType,
            type.Kind
        );
    }
}