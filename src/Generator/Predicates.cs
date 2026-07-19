using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator;

internal static class Predicates
{
    public static bool IsClassNode(SyntaxNode node, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return node is ClassDeclarationSyntax;
    }

    public static bool IsPropertyOrFieldNode(SyntaxNode node, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return node is PropertyDeclarationSyntax or VariableDeclaratorSyntax;
    }

    public static bool IsEnumWithAttrib(SyntaxNode node, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return node is EnumDeclarationSyntax { AttributeLists.Count: > 0 };
    }
}