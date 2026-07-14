using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Generator;

internal static class Extensions
{
    public static string ToFloatStr(this float v) => $"{v}f";

    public static bool IsPublicClassOrStruct(this INamedTypeSymbol sym)
        => sym.TypeKind is TypeKind.Class or TypeKind.Struct && sym.DeclaredAccessibility == Accessibility.Public;

    public static ITypeSymbol GetFieldOrPropertyType(this ISymbol sym) => sym switch
    {
        IFieldSymbol field => field.Type,
        IPropertySymbol property => property.Type,
        _ => throw new UnreachableException()
    };

    public static string ToCollectionString(this ImmutableArray<TypedConstant> values)
    {
        return $"[{string.Join(", ", values.Select(v => v.ToCSharpString()))}]";
    }


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
    
    public static IEnumerable<INamedTypeSymbol> GetAllTypes(this INamespaceSymbol ns)
    {
        foreach (var member in ns.GetMembers())
        {
            if (member is INamespaceSymbol nested)
                foreach (var t in GetAllTypes(nested))
                    yield return t;
            else if (member is INamedTypeSymbol type)
                yield return type;
        }
    }

}