using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator;

internal static class Predicates
{
    public static bool IsPropertyOrField(SyntaxNode node, CancellationToken _) =>
        node is PropertyDeclarationSyntax or VariableDeclaratorSyntax;

    public static bool IsEnumWithAttrib(SyntaxNode node, CancellationToken _) =>
        node is EnumDeclarationSyntax { AttributeLists.Count: > 0 };
}