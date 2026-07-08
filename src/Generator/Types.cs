using Microsoft.CodeAnalysis;

namespace Generator;


internal record struct MemberTypeInfo(
    bool IsValueType,
    bool IsUnmanaged,
    bool IsReadOnly,
    TypeKind TypeKind,
    SpecialType SpecialType)
{
    private static MemberTypeInfo ExtractMemberTypeInfo(ISymbol sym)
    {
        var type = sym.GetFieldOrPropertyType();
        
        return new MemberTypeInfo(
            type.IsValueType,
            type.IsUnmanagedType,
            type.IsReadOnly,
            type.TypeKind,
            type.SpecialType
        );
    }
}
