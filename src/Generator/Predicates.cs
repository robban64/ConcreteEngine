using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator;

internal static class Predicates
{
    public static bool IsObjectOrStruct(SyntaxNode node, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return node is ClassDeclarationSyntax or InterfaceDeclarationSyntax or StructDeclarationSyntax;
    }

    public static bool IsPropertyOrField(SyntaxNode node, CancellationToken ct)
    {
        return node is PropertyDeclarationSyntax or VariableDeclaratorSyntax;
    }

    public static bool IsEnumWithAttrib(SyntaxNode node, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return node is EnumDeclarationSyntax { AttributeLists.Count: > 0 };
    }
}