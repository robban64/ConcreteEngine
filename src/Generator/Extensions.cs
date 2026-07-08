using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Generator;


internal static class Extensions
{

    public static bool IsPublicClassOrStruct(this INamedTypeSymbol sym)
        => sym.TypeKind is TypeKind.Class or TypeKind.Struct && sym.DeclaredAccessibility == Accessibility.Public;

    public static ITypeSymbol GetFieldOrPropertyType(this ISymbol sym) => sym switch
    {
        IFieldSymbol field => field.Type,
        IPropertySymbol property => property.Type,
        _ => throw new UnreachableException()
    };
    
  

    public static bool TryGetConst<T>(this ISymbol sym, out T? value)
    {
        if (sym is IFieldSymbol { HasConstantValue: true, ConstantValue: T v })
        {
            value = v;
            return true;
        }
        value = default;
        return false;
    }

}