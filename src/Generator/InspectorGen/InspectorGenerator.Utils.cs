using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generator.InspectorGen;

public sealed partial class InspectorGenerator
{
    private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol ns)
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


    private static readonly Dictionary<string, string> DefaultInputMap = new()
    {
        { "Vector2", "Float2" },
        { "Vector3", "Float3" },
        { "Vector4", "Float4" },
        { "Color4", "Float4" },
        
        { "Size2D", "Int2" },
        { "Vector2I", "Int2" }
    };
    
    internal enum SupportedTypes
    {
        Int,
        Float,
        Bool,
        DateTime,
    }
    
    
    private static MemberTypeInfo ExtractMemberTypeInfo(ISymbol sym)
    {
        var type = sym switch
        {
            IFieldSymbol field => field.Type,
            IPropertySymbol property => property.Type,
            _ => throw new UnreachableException()
        };
        return new MemberTypeInfo(
            type.IsValueType,
            type.IsUnmanagedType,
            type.IsReadOnly,
            type.TypeKind,
            type.SpecialType
        );
    }
    
  
    private static IInspectField? MakeField(AttributeData attr, ITypeSymbol type)
    {
        if (attr.ConstructorArguments.IsEmpty || attr.ConstructorArguments[0].Value is not string typeName)
        {
            var s = type.SpecialType;
            if (s == SpecialType.System_Enum) typeName = "Int1";
            else if (s == SpecialType.System_Single) typeName = "Float1";
            else if (s == SpecialType.System_Int32) typeName = "Int1";
            else if (DefaultInputMap.TryGetValue(type.Name, out var defaultValue)) typeName = defaultValue!;
            else return null;
        }

        bool hasAlpha = true;

        int startAt = 0;
        string? placeholder = null;

        string? format = null;
        float min = 0, max = 0, speed = 0;
        foreach (var kv in attr.NamedArguments)
        {
            var v = kv.Value.Value;
            switch (kv.Key)
            {
                case "Min" when v is float l: min = l; break;
                case "Max" when v is float l: max = l; break;
                case "Speed" when v is float l: speed = l; break;
                case "Format" when v is string l: format = SymbolDisplay.FormatLiteral(l, quote: true); break;
                case "HasAlpha" when v is bool l: hasAlpha = l; break;
                case "StartAt" when v is int l: startAt = l; break;
                case "Placeholder" when v is string l: placeholder = l; break;
            }
        }

        return attr.AttributeClass?.Name switch
        {
            "ColorFieldAttribute" => new ColorField { HasAlpha = hasAlpha },
            "ComboFieldAttribute" => new ComboField { Placeholder = placeholder, StartAt = startAt },
            InputAttribName => new InputField
            {
                ValueType = typeName,
                Format = format,
                Min = min,
                Max = max,
                Speed = speed
            },
            _ => null
        };
    }
}